from dataclasses import dataclass


@dataclass(frozen=True)
class SqlConnectionSettings:
    server: str
    database: str
    user: str
    password: str
    port: int = 1433
    login_timeout: int = 30

    @staticmethod
    def from_connection_string(connection_string: str) -> "SqlConnectionSettings":
        if connection_string.startswith("jdbc:sqlserver://"):
            return _parse_jdbc_sql_server_connection_string(connection_string)

        raise ValueError("Only JDBC SQL Server connection strings are supported for now.")


def _parse_jdbc_sql_server_connection_string(connection_string: str) -> SqlConnectionSettings:
    without_prefix = connection_string.removeprefix("jdbc:sqlserver://").strip().strip(";")
    server_part, *option_parts = without_prefix.split(";")

    server, port = _parse_server_and_port(server_part)
    options = _parse_options(option_parts)

    database = options.get("database") or options.get("databasename")
    user = options.get("user")
    password = options.get("password")
    login_timeout = int(options.get("logintimeout", "30"))

    if not database:
        raise ValueError("Connection string is missing database.")
    if not user:
        raise ValueError("Connection string is missing user.")
    if not password:
        raise ValueError("Connection string is missing password.")

    return SqlConnectionSettings(
        server=server,
        port=port,
        database=database,
        user=user,
        password=password,
        login_timeout=login_timeout,
    )


def _parse_server_and_port(server_part: str) -> tuple[str, int]:
    if ":" not in server_part:
        return server_part, 1433

    server, port_text = server_part.rsplit(":", maxsplit=1)
    return server, int(port_text)


def _parse_options(option_parts: list[str]) -> dict[str, str]:
    options: dict[str, str] = {}
    for option_part in option_parts:
        if not option_part or "=" not in option_part:
            continue

        key, value = option_part.split("=", maxsplit=1)
        options[key.strip().lower()] = value.strip()

    return options

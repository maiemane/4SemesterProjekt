from hedging_engine.infrastructure.repositories.sql_connection_settings import SqlConnectionSettings


def test_parses_jdbc_sql_server_connection_string() -> None:
    settings = SqlConnectionSettings.from_connection_string(
        "jdbc:sqlserver://example.database.windows.net:1433;"
        "database=Hedging;"
        "user=test-user;"
        "password=test-password;"
        "encrypt=true;"
        "loginTimeout=30;"
    )

    assert settings.server == "example.database.windows.net"
    assert settings.port == 1433
    assert settings.database == "Hedging"
    assert settings.user == "test-user"
    assert settings.password == "test-password"
    assert settings.login_timeout == 30

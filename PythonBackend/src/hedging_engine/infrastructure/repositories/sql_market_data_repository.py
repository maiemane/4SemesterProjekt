import pandas as pd
import pymssql

from hedging_engine.application.ports.market_data_repository import MarketDataRepository
from hedging_engine.domain.models import DateRange
from hedging_engine.infrastructure.repositories.sql_connection_settings import SqlConnectionSettings


class SqlMarketDataRepository(MarketDataRepository):
    """Read-only SQL Server adapter for market data."""

    def __init__(self, connection_string: str) -> None:
        self._settings = SqlConnectionSettings.from_connection_string(connection_string)

    def load_fund_nav_values(self, fund_ticker: str, period: DateRange) -> pd.Series:
        rows = self._fetch_all(
            """
            SELECT dn.nav_date, dn.nav
            FROM DAILY_NAV dn
            INNER JOIN INSTRUMENT i ON i.instrument_id = dn.instrument_id
            WHERE i.bloomberg_ticker = %s
              AND dn.nav_date >= %s
              AND dn.nav_date <= %s
            ORDER BY dn.nav_date
            """,
            (fund_ticker, period.start, period.end),
        )

        return _rows_to_series(rows, date_column="nav_date", value_column="nav", series_name=fund_ticker)

    def load_hedge_price_values(
        self,
        candidate_tickers: tuple[str, ...],
        period: DateRange,
    ) -> pd.DataFrame:
        if not candidate_tickers:
            return pd.DataFrame()

        placeholders = ", ".join(["%s"] * len(candidate_tickers))
        rows = self._fetch_all(
            f"""
            SELECT i.bloomberg_ticker, dp.trade_date, dp.price
            FROM DAILY_PRICE dp
            INNER JOIN INSTRUMENT i ON i.instrument_id = dp.instrument_id
            WHERE i.bloomberg_ticker IN ({placeholders})
              AND dp.trade_date >= %s
              AND dp.trade_date <= %s
            ORDER BY dp.trade_date, i.bloomberg_ticker
            """,
            (*candidate_tickers, period.start, period.end),
        )

        if not rows:
            return pd.DataFrame(columns=candidate_tickers)

        frame = pd.DataFrame(rows)
        frame["trade_date"] = pd.to_datetime(frame["trade_date"])
        frame["price"] = frame["price"].astype(float)

        pivoted = frame.pivot_table(
            index="trade_date",
            columns="bloomberg_ticker",
            values="price",
            aggfunc="last",
        ).sort_index()

        return pivoted.reindex(columns=list(candidate_tickers))

    def load_available_price_tickers(self, limit: int = 25) -> tuple[str, ...]:
        rows = self._fetch_all(
            """
            SELECT TOP (%d) i.bloomberg_ticker
            FROM INSTRUMENT i
            WHERE EXISTS (
                SELECT 1
                FROM DAILY_PRICE dp
                WHERE dp.instrument_id = i.instrument_id
            )
            ORDER BY i.bloomberg_ticker
            """
            % limit,
            (),
        )

        return tuple(str(row["bloomberg_ticker"]) for row in rows)

    def _fetch_all(self, query: str, parameters: tuple[object, ...]) -> list[dict[str, object]]:
        with pymssql.connect(
            server=self._settings.server,
            port=self._settings.port,
            database=self._settings.database,
            user=self._settings.user,
            password=self._settings.password,
            login_timeout=self._settings.login_timeout,
            as_dict=True,
            autocommit=True,
            read_only=True,
            encryption="require",
        ) as connection:
            with connection.cursor() as cursor:
                cursor.execute(query, parameters)
                return list(cursor.fetchall())


def _rows_to_series(
    rows: list[dict[str, object]],
    date_column: str,
    value_column: str,
    series_name: str,
) -> pd.Series:
    if not rows:
        return pd.Series(dtype=float, name=series_name)

    frame = pd.DataFrame(rows)
    frame[date_column] = pd.to_datetime(frame[date_column])
    frame[value_column] = frame[value_column].astype(float)

    series = frame.set_index(date_column)[value_column].sort_index()
    series.name = series_name
    return series

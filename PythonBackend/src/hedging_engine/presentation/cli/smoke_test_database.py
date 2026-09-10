import os
from datetime import date

from hedging_engine.domain.models import DateRange
from hedging_engine.infrastructure.repositories.sql_market_data_repository import SqlMarketDataRepository


def main() -> None:
    connection_string = os.environ.get("HEDGING_DB_CONNECTION_STRING")
    if not connection_string:
        raise SystemExit("Missing HEDGING_DB_CONNECTION_STRING.")

    repository = SqlMarketDataRepository(connection_string)
    period = DateRange(start=date(2021, 1, 1), end=date(2026, 12, 31))

    fund_nav = repository.load_fund_nav_values("ALW LN Equity", period)
    hedge_prices = repository.load_hedge_price_values(
        ("QQQ", "IVV", "MCHI", "ACWI", "EWT", "EEM", "GBPUSD", "IWM"),
        period,
    )

    print(f"Fund NAV rows: {len(fund_nav)}")
    print(f"Hedge price rows: {len(hedge_prices)}")
    print(f"Hedge columns: {', '.join(hedge_prices.columns)}")
    if not fund_nav.empty:
        print(f"NAV date range: {fund_nav.index.min().date()} -> {fund_nav.index.max().date()}")
    if not hedge_prices.empty:
        print(f"Price date range: {hedge_prices.index.min().date()} -> {hedge_prices.index.max().date()}")
    else:
        available_tickers = repository.load_available_price_tickers()
        if available_tickers:
            print("No hedge price rows found for the hardcoded candidates.")
            print(f"Example DAILY_PRICE tickers: {', '.join(available_tickers)}")
        else:
            print("No DAILY_PRICE rows found in the database.")


if __name__ == "__main__":
    main()

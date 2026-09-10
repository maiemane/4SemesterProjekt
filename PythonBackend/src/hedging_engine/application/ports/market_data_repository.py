from abc import ABC, abstractmethod

import pandas as pd

from hedging_engine.domain.models import DateRange


class MarketDataRepository(ABC):
    """Port for loading market data without exposing SQL details."""

    @abstractmethod
    def load_fund_nav_values(self, fund_ticker: str, period: DateRange) -> pd.Series:
        """Load the fund NAV level series used as the target input."""

    @abstractmethod
    def load_hedge_price_values(
        self,
        candidate_tickers: tuple[str, ...],
        period: DateRange,
    ) -> pd.DataFrame:
        """Load price level series for candidate hedge instruments."""

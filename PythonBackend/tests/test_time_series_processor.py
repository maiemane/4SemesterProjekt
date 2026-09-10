import pandas as pd
import pytest

from hedging_engine.application.services.time_series_processor import TimeSeriesProcessor
from hedging_engine.domain.models import MissingDataPolicy


def test_prepare_aligned_returns_drops_missing_dates() -> None:
    processor = TimeSeriesProcessor()
    dates = pd.to_datetime(["2026-01-01", "2026-01-02", "2026-01-03"])
    fund_nav = pd.Series([100.0, 110.0, 121.0], index=dates)
    hedge_prices = pd.DataFrame(
        {
            "QQQ": [200.0, 220.0, 242.0],
            "IVV": [300.0, 330.0, 363.0],
        },
        index=dates,
    )

    target_returns, hedge_returns = processor.prepare_aligned_returns(
        fund_nav,
        hedge_prices,
        MissingDataPolicy.DROP_DATES,
    )

    assert target_returns.tolist() == pytest.approx([0.1, 0.1])
    assert hedge_returns["QQQ"].tolist() == pytest.approx([0.1, 0.1])
    assert hedge_returns["IVV"].tolist() == pytest.approx([0.1, 0.1])


def test_prepare_aligned_returns_forward_fills_missing_values() -> None:
    processor = TimeSeriesProcessor()
    dates = pd.to_datetime(["2026-01-01", "2026-01-02", "2026-01-03"])
    fund_nav = pd.Series([100.0, 110.0, 121.0], index=dates)
    hedge_prices = pd.DataFrame({"QQQ": [200.0, None, 242.0]}, index=dates)

    _, hedge_returns = processor.prepare_aligned_returns(
        fund_nav,
        hedge_prices,
        MissingDataPolicy.FORWARD_FILL,
    )

    assert hedge_returns["QQQ"].tolist() == pytest.approx([0.0, 0.21])

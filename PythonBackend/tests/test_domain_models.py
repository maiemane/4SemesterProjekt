from datetime import date

import pytest

from hedging_engine.domain.models import DateRange, HedgeCandidate, HedgeOptimizationRequest


def test_date_range_rejects_end_before_start() -> None:
    with pytest.raises(ValueError):
        DateRange(start=date(2026, 1, 2), end=date(2026, 1, 1))


def test_hedge_candidate_requires_ticker() -> None:
    with pytest.raises(ValueError):
        HedgeCandidate(ticker="")


def test_request_requires_candidates() -> None:
    with pytest.raises(ValueError):
        HedgeOptimizationRequest(
            fund_ticker="ALW LN Equity",
            period=DateRange(start=date(2026, 1, 1), end=date(2026, 2, 1)),
            candidates=(),
        )

# Common date-range
import warnings
import pandas as pd

from hedging_engine.domain.models import MissingDataPolicy

fund_dates = fund_nav_values.index
hedge_dates = hedge_price_values.index

common_dates = fund_dates.intersection(hedge_dates)

if len(common_dates) == 0:
    warnings.append("No overlapping dates between fund NAV and hedge prices")
    return fund_nav_values, hedge_price_values, warnings

# Normalisering timezone
def _normalize_timezone(index):
    if index.tz is not None:
        return index.tz_convert("UTC")
    return index

fund_nav_values.index = _normalize_timezone(fund_nav_values.index)
hedge_price_values.index = _normalize_timezone(hedge_price_values.index)

# Missing data detection
nav_missing = fund_nav_values.isna().sum()
hedge_missing = hedge_price_values.isna().sum().sum()

if nav_missing > 0:
    warnings.append(f"Fund Nav contains {nav_missing} missing values.")

if hedge_missing > 0:
    warnings.append(f"Hedge price matrix contains {hedge_missing} missing values.")

# Missing data policy
if missing_data_policy == MissingDataPolicy.STRICT:
    if nav_missing > 0 or hedge_missing > 0:
        warnings.append(
            "Missing data detected. Validation stopped withput modifying data."
        )
        return fund_nav_values, hedge_price_values, warnings

elif missing_data_policy == MissingDataPolicy.DROP:
    combined = pd.concat([fund_nav_values, hedge_price_values], axis=1)
    before = len(combined)

    combined = combined.dropna()

    after = len(combined)
    dropped = before - after
    if dropped > 0:
        warnings.append(
            "Dropped data detected. {dropped} rows with missing data were removed."
        )
    fund_nav_values = combined.iloc[:, 0]
    hedge_price_values = combined.iloc[:, 1:]

elif missing_data_policy == MissingDataPolicy.FORWARD_FILL:
    before_nav_missing = fund_nav_values.isna().sum()
    before_hedge_missing = hedge_price_values.isna().sum().sum()

    fund_nav_values = fund_nav_values.ffill()
    hedge_price_values = hedge_price_values.ffill()

    after_nav_missing = fund_nav_values.isna().sum()
    after_hedge_missing = hedge_price_values.isna().sum().sum()

    filled_nav = before_nav_missing - after_nav_missing
    filled_hedge = before_hedge_missing - after_hedge_missing

    if filled_nav > 0 or filled_hedge > 0:
        warnings.append(
            "Policy applied"
        )

elif missing_data_policy == MissingDataPolicy.INTERPOLATE:
    before_nav_missing = fund_nav_values.isna().sum()
    before_hedge_missing = hedge_price_values.isna().sum().sum()

    fund_nav_values = fund_nav_values.interpolate(method="linear")
    hedge_price_values = hedge_price_values.interpolate(method="linear", axis=0)

    after_nav_missing = fund_nav_values.isna().sum()
    after_hedge_missing = hedge_price_values.isna().sum().sum()

    interpolated_nav = before_nav_missing - after_nav_missing
    interpolated_hedge = before_hedge_missing - after_hedge_missing

    if interpolated_nav > 0 or interpolated_hedge > 0:
        warnings.append(
            f"INTERPOLATE policy applied: "
            f"{interpolated_nav} NAV values and {interpolated_hedge} hedge values were interpolated."
        )
return fund_nav_values, hedge_price_values, warnings
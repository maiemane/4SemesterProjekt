from hedging_engine.domain.models import MissingDataPolicy
import warnings
import pandas as pd

def validate(self, fund_nav_values, hedge_price_values, missing_data_policy: MissingDataPolicy):

    # Common date-range
    fund_dates = fund_nav_values.index
    hedge_dates = hedge_price_values.index

    common_dates = fund_dates.intersection(hedge_dates)

    if len(common_dates) == 0:
        warnings.append("No overlapping dates between fund NAV and hedge prices")
        return fund_nav_values, hedge_price_values, warnings

    fund_nav_values = fund_nav_values.loc[common_dates]
    hedge_price_values = hedge_price_values.loc[common_dates]

    # Normalisering timezone
    def _normalize_timezone(index):
        if index.tz is not None:
            return index.tz_convert("UTC")
        return index

    fund_nav_values.index = _normalize_timezone(fund_nav_values.index)
    hedge_price_values.index = _normalize_timezone(hedge_price_values.index)

    if fund_nav_values.index.tz is None and hedge_price_values.index.tz is not None:
        fund_nav_values.index = fund_nav_values.index.tz_localize("UTC")

    if hedge_price_values.index.tz is None and fund_nav_values.index.tz is not None:
        hedge_price_values.index = hedge_price_values.index.tz_localize("UTC")

    # Missing data detection
    nav_missing = fund_nav_values.isna().sum()
    hedge_missing = hedge_price_values.isna().sum().sum()

    if nav_missing > 0:
        warnings.append(f"Fund NAV contains {nav_missing} missing values.")

    if hedge_missing > 0:
        warnings.append(f"Hedge price matrix contains {hedge_missing} missing values.")

    # Missing data policy
    if missing_data_policy == MissingDataPolicy.STRICT:
        if nav_missing > 0 or hedge_missing > 0:
            warnings.append(
                "Missing data detected. Validation stopped without modifying data."
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
                f"DROP policy applied: {dropped} rows with missing data were removed."
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
                f"FORWARD_FILL policy applied: {filled_nav} NAV values and {filled_hedge} hedge values were forward-filled."
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
                f"INTERPOLATE policy applied: {interpolated_nav} NAV values and {interpolated_hedge} hedge values were interpolated."
            )

    return fund_nav_values, hedge_price_values, warnings #

    # Outliner detection
    fund_returns = fund_nav_values.pct_change().dropna()
    hedge_returns = hedge_price_values.pct_change().dropna()

    lower_fund = fund_returns.quantile(0.025)
    upper_fund = fund_returns.quantile(0.975)

    lower_fund = fund_returns.quantile(0.025)
    upper_fund = fund_returns.quantile(0.975)

    fund_outliers = (fund_returns < lower_fund | fund_returns > upper_fund)
    hedge_outliers = (hedge_returns < lower_hedge | hedge_returns > upper_hedge)

    num_fund_outliers = fund_outliers.sum()
    num_hedge_outliers = hedge_outliers.sum().sum()

    if num_fund_outliers > 0:
        warnings.append(f"Detected {num_fund_outliers} outliers in fund returns.")

    if num_hedge_outliers > 0:
        warnings.append(f"Detected {num_hedge_outliers} outliers in fund returns.")
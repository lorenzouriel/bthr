"""Pure usage normalization and non-overlapping token-price arithmetic."""
from decimal import Decimal, InvalidOperation

def categories(usage):
    if not usage or "input_tokens" not in usage or "output_tokens" not in usage:
        return None
    details = usage.get("input_token_details") or {}
    read, write = details.get("cache_read", 0), details.get("cache_creation", 0)
    if any(type(value) is not int or value < 0 for value in (usage["input_tokens"], usage["output_tokens"], read, write)):
        return None
    uncached = usage["input_tokens"] - read - write
    values = {"input": uncached, "output": usage["output_tokens"], "cache_read": read, "cache_write": write}
    if any(not isinstance(v, int) or v < 0 for v in values.values()):
        return None
    return values


def estimate_cost(usage, rate):
    if usage is None or rate is None:
        return None
    if any(amount and category not in rate for category, amount in usage.items()):
        return None
    try:
        if any(not Decimal(str(rate.get(category, 0))).is_finite() or Decimal(str(rate.get(category, 0))) < 0 for category in usage):
            return None
        return sum(Decimal(amount) * Decimal(str(rate.get(category, 0))) / Decimal(1000000)
                   for category, amount in usage.items())
    except (InvalidOperation, ValueError, TypeError):
        return None



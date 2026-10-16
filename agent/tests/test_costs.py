import unittest
from decimal import Decimal
from bthr_agent.costs import categories, estimate_cost


class CostTests(unittest.TestCase):
    def test_cache_is_a_subset_not_extra_input(self):
        usage = categories({"input_tokens": 1000, "output_tokens": 100,
                            "input_token_details": {"cache_read": 600, "cache_creation": 100}})
        self.assertEqual(usage, {"input": 300, "output": 100, "cache_read": 600, "cache_write": 100})
        self.assertEqual(estimate_cost(usage, {"input": 2, "output": 8, "cache_read": 0.2, "cache_write": 3}), Decimal("0.00182"))

    def test_unknown_is_not_free(self):
        self.assertIsNone(categories(None))
        self.assertIsNone(estimate_cost({"input": 100}, None))
        self.assertIsNone(estimate_cost({"cache_read": 100}, {"input": 1}))

    def test_invalid_counts_are_unknown(self):
        self.assertIsNone(categories({"input_tokens": 10, "output_tokens": 1, "input_token_details": {"cache_read": 11}}))
        self.assertIsNone(categories({"input_tokens": "10", "output_tokens": 1}))

    def test_reasoning_subset_is_not_added_again(self):
        self.assertEqual(categories({"input_tokens": 10, "output_tokens": 20,
                                     "output_token_details": {"reasoning": 15}})["output"], 20)

    def test_zero_reported_usage_is_known(self):
        self.assertEqual(estimate_cost(categories({"input_tokens": 0, "output_tokens": 0}), {}), Decimal(0))

    def test_invalid_rates_are_unknown(self):
        self.assertIsNone(estimate_cost({"input": 1}, {"input": -1}))
        self.assertIsNone(estimate_cost({"input": 1}, {"input": "NaN"}))


if __name__ == "__main__":
    unittest.main()

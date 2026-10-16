import io
import warnings
from datetime import date
from decimal import Decimal
from pydantic import BaseModel, ConfigDict, Field
from PIL import Image, ImageOps

MAX_BYTES = 10 * 1024 * 1024
Image.MAX_IMAGE_PIXELS = 20_000_000


def normalize(data: bytes) -> bytes:
    if len(data) > MAX_BYTES:
        raise ValueError("Image is larger than 10 MB")
    with warnings.catch_warnings():
        warnings.simplefilter("error", Image.DecompressionBombWarning)
        with Image.open(io.BytesIO(data)) as original:
            if original.format not in ("JPEG", "PNG", "WEBP") or getattr(original, "n_frames", 1) != 1:
                raise ValueError("Use a JPEG, PNG or single-frame WebP image")
            original.load()
            image = ImageOps.exif_transpose(original).convert("RGB")
            image.thumbnail((2048, 2048))
            output = io.BytesIO()
            image.save(output, format="JPEG", quality=90)
            return output.getvalue()


class MealExtraction(BaseModel):
    model_config = ConfigDict(extra="forbid")
    is_meal: bool
    question: str = Field(default="", max_length=500)
    description: str = Field(default="", max_length=500)
    meal_date: date | None = None
    meal_type: str = Field(default="Snack", min_length=1, max_length=50)
    calories: Decimal | None = Field(default=None, ge=0, le=999999.99, decimal_places=2)
    protein_grams: Decimal | None = Field(default=None, ge=0, le=9999.99, decimal_places=2)
    carbs_grams: Decimal | None = Field(default=None, ge=0, le=9999.99, decimal_places=2)
    fat_grams: Decimal | None = Field(default=None, ge=0, le=9999.99, decimal_places=2)

    def payload(self, today):
        return {"mealDate": str(self.meal_date or today) + "T00:00:00Z", "mealType": self.meal_type,
                "description": self.description, "calories": float(self.calories) if self.calories is not None else None,
                **{key: float(value) for key, value in (("proteinGrams", self.protein_grams),
                   ("carbsGrams", self.carbs_grams), ("fatGrams", self.fat_grams)) if value is not None}}

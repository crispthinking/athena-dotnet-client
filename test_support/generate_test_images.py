#!/usr/bin/env -S uv run --script
#
# /// script
# requires-python = ">=3.10"
# dependencies = ["pillow"]
# ///
"""
Generate 1000 simple test JPG images for Athena CLI testing.

Use:

python generate_test_images.py > my_imagefile.txt
athena-cli classify -i my_imagefile.txt

"""

import os
from PIL import Image, ImageDraw, ImageFont
import random

def generate_test_images(output_dir, count=1000):
    """Generate simple test images with different colors and text."""

    # Ensure output directory exists
    os.makedirs(output_dir, exist_ok=True)

    # Colors for background
    colors = [
        (255, 0, 0),      # Red
        (0, 255, 0),      # Green
        (0, 0, 255),      # Blue
        (255, 255, 0),    # Yellow
        (255, 0, 255),    # Magenta
        (0, 255, 255),    # Cyan
        (255, 128, 0),    # Orange
        (128, 0, 255),    # Purple
        (255, 192, 203),  # Pink
        (0, 128, 0),      # Dark Green
        (128, 128, 128),  # Gray
        (0, 0, 0),        # Black
        (255, 255, 255),  # White
        (128, 0, 0),      # Maroon
        (0, 128, 128),    # Teal
        (128, 128, 0),    # Olive
        (0, 0, 128),      # Navy
        (210, 105, 30),   # Chocolate
        (255, 215, 0),    # Gold
        (70, 130, 180),   # Steel Blue
    ]

    import sys
    if count < 0:
        count = sys.maxsize
        print(f"# Image Test file with unlimited images (infinite mode)")
    else:
        print(f"# Image Test file with {count} images")

    for i in range(count):
        img = Image.new('RGB', (448, 448), random.choice(colors))
        draw = ImageDraw.Draw(img)

        if i % 3 == 0:
            draw.ellipse([20, 20, 80, 80], fill=(255, 255, 255))
        elif i % 3 == 1:
            draw.rectangle([20, 20, 80, 80], fill=(255, 255, 255))
        else:
            draw.polygon([(50, 20), (20, 80), (80, 80)], fill=(255, 255, 255))

        try:
            font = ImageFont.load_default()
            draw.text((10, 10), str(i), fill=(0, 0, 0), font=font)
        except:
            draw.text((10, 10), str(i), fill=(0, 0, 0))

        filename = f"test_image_{i:04d}.jpg"
        filepath = os.path.join(output_dir, filename)
        img.save(filepath, "JPEG", quality=85)
        print(f"{filepath}")

        if (i + 1) % 100 == 0:
            print(f"# Generated {i + 1} images...")

    if count != sys.maxsize:
        print(f"# Successfully generated {count} test images in {output_dir}")

if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser(description="Generate test JPG images for Athena CLI testing.")
    parser.add_argument("-n", "--num", type=int, default=1000, help="Number of images to generate (default: 1000)")
    parser.add_argument("-o", "--output", type=str, default="images", help="Output directory (default: images)")
    args = parser.parse_args()

    generate_test_images(args.output, args.num)

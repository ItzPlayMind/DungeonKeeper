import json
import os

# Input JSON file containing all items
input_file = "items.json"

# Folder to save separate JSON files
output_folder = "Items"
os.makedirs(output_folder, exist_ok=True)

# Read the items from the input file
with open(input_file, "r") as f:
    items = json.load(f)

# Create a JSON file for each item
for key, value in items.items():
    filename = f"{output_folder}/{key}.json"
    with open(filename, "w") as f:
        json.dump(value, f, indent=4)

print(f"Created {len(items)} JSON files in '{output_folder}' folder.")

import os, json

p = "witte_dataset.jsonl"
print(f"Size: {os.path.getsize(p)} bytes")
lines = open(p, "r", encoding="utf-8").readlines()
print(f"Entries: {len(lines)}")
d = json.loads(lines[5])
print(f"Sample Q: {d['instruction']}")
print(f"Sample A: {d['output'][:300]}")
print()
d2 = json.loads(lines[10])
print(f"Sample Q2: {d2['instruction']}")
print(f"Sample A2: {d2['output'][:300]}")

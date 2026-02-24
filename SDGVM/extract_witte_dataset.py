import os, re, json, random

INPUT_DIR = "Vitte_text"
OUTPUT_FILE = "witte_dataset.jsonl"

# Read all files with forced cp1251
all_text = ""
for f in sorted(os.listdir(INPUT_DIR)):
    if f.endswith(".txt"):
        path = os.path.join(INPUT_DIR, f)
        with open(path, "rb") as fh:
            raw = fh.read()
        text = raw.decode("cp1251", errors="replace")
        all_text += "\n\n" + text
        rc = sum(1 for c in text[:500] if "\u0400" <= c <= "\u04ff")
        print(f"Read {f}: {len(text)} chars, Russian chars in first 500: {rc}")

print(f"\nTotal text: {len(all_text)} chars ({len(all_text)/1024/1024:.1f} MB)")

# Clean
all_text = re.sub(r"\n{3,}", "\n\n", all_text)
all_text = re.sub(r" {2,}", " ", all_text)

# Split into paragraphs
paras = [p.strip() for p in re.split(r"\n\s*\n", all_text) if len(p.strip()) > 100]
print(f"Paragraphs (>100 chars): {len(paras)}")

# Topic keywords
TOPICS = {
    "reforms": ["реформ", "бюджет", "финанс", "налог", "промышленн", "капитал", "банк", "тариф", "казн"],
    "gold": ["золот", "рубл", "валют", "денеж", "стандарт", "монет", "курс"],
    "transsib": ["дорог", "железн", "сибир", "магистрал", "рельс", "паровоз", "путь", "маньчжур"],
    "tsar": ["государ", "император", "николай", "царь", "величеств", "дворец", "аудиенц"],
    "manifesto": ["манифест", "конституц", "дума", "свобод", "выбор", "октябр", "революц", "стачк"],
    "wine": ["винн", "водк", "монопол", "акциз", "питейн"],
    "education": ["универс", "студент", "образован", "профессор", "наук", "академи", "школ"],
    "foreign": ["войн", "мир", "договор", "дипломат", "посол", "портсмут", "япон"],
    "character": ["характер", "убежден", "принцип", "считал", "полагал", "верил", "стремил"],
    "workers": ["рабоч", "фабричн", "стачк", "забастов", "заработн", "пролетариат"],
}

QUESTIONS = {
    "reforms": ["Расскажи об экономических реформах Витте.", "Какие экономические преобразования проводил Витте?", "Как Витте развивал российскую экономику?", "Что сделал Витте для промышленности России?"],
    "gold": ["Расскажи о введении золотого стандарта.", "Как Витте укрепил рубль?", "Зачем Витте ввёл золотой стандарт?", "Что такое денежная реформа Витте?"],
    "transsib": ["Расскажи о строительстве Транссиба.", "Зачем Витте строил Транссибирскую магистраль?", "Какое значение имел Транссиб для России?"],
    "tsar": ["Как Витте относился к Николаю II?", "Какие были отношения между Витте и царём?", "Расскажи о конфликтах Витте с императором."],
    "manifesto": ["Расскажи о Манифесте 17 октября.", "Как появилась Государственная дума?", "Какую роль Витте сыграл в Манифесте?"],
    "wine": ["Расскажи о винной монополии Витте.", "Зачем Витте ввёл винную монополию?", "Как монополия повлияла на бюджет?"],
    "education": ["Как Витте относился к образованию?", "Что Витте сделал для университетов?", "Расскажи о студенческом движении."],
    "foreign": ["Расскажи о внешней политике Витте.", "Как Витте заключил Портсмутский мир?", "Как Витте относился к войне с Японией?"],
    "character": ["Опиши характер Витте.", "Каким человеком был Витте?", "Какие принципы были у Витте?"],
    "workers": ["Как Витте решал рабочий вопрос?", "Что Витте думал о рабочем движении?", "Расскажи о фабричном законодательстве."],
}

GENERIC = ["Расскажи об этом периоде.", "Что ты знаешь об этом?", "Опиши этот эпизод.", "Расскажи подробнее."]

def classify(text):
    tl = text.lower()
    best, best_score = None, 0
    for topic, kws in TOPICS.items():
        score = sum(1 for kw in kws if kw in tl)
        if score > best_score:
            best, best_score = topic, score
    return best if best_score >= 2 else None

# Build chunks
MIN_LEN, MAX_LEN = 200, 1200
chunks = []
cur = ""
for p in paras:
    if len(cur) + len(p) < MAX_LEN:
        cur = (cur + " " + p).strip() if cur else p
    else:
        if len(cur) >= MIN_LEN:
            chunks.append(cur)
        cur = p
if len(cur) >= MIN_LEN:
    chunks.append(cur)

print(f"Chunks: {len(chunks)}")

# Handcrafted examples
dataset = [
    {"instruction": "Ты - дух университета Витте. Представься.", "input": "", "output": "Добро пожаловать! Я - хранитель памяти Сергея Юльевича Витте, великого реформатора Российской империи. Он верил, что сильная Россия начинается с образованных людей. Чем могу помочь?"},
    {"instruction": "Кто такой Витте?", "input": "", "output": "Сергей Юльевич Витте - один из величайших государственных деятелей Российской империи. Министр финансов с 1892 по 1903 год, затем первый председатель Совета министров. Провёл денежную реформу, ввёл золотой стандарт, построил Транссиб и добился Манифеста 17 октября 1905 года."},
    {"instruction": "Почему Витте важен для России?", "input": "", "output": "Витте заложил основы индустриальной России. Золотой стандарт сделал рубль одной из устойчивых валют мира. Транссиб связал европейскую часть с Дальним Востоком. Винная монополия наполнила бюджет. Без Витте модернизация России была бы невозможна."},
    {"instruction": "Витте был хорошим человеком?", "input": "", "output": "Витте был прежде всего прагматиком. Он не стремился быть хорошим - он стремился быть полезным для России. Многие его не любили: дворяне считали выскочкой, революционеры - слугой режима. Но даже враги признавали его ум."},
]

topic_counts = {}
for chunk in chunks:
    topic = classify(chunk)
    # Trim to last sentence
    out = chunk.strip()
    if len(out) > MAX_LEN:
        sents = re.split(r"(?<=[.!?])\s+", out[:MAX_LEN])
        if len(sents) > 1:
            out = " ".join(sents[:-1])
    out = re.sub(r"^[ИиНнАа]\s+", "", out)
    if out and out[-1] not in ".!?":
        out += "."
    
    q = random.choice(QUESTIONS.get(topic, GENERIC))
    dataset.append({"instruction": q, "input": "", "output": out})
    
    tn = topic or "other"
    topic_counts[tn] = topic_counts.get(tn, 0) + 1

random.shuffle(dataset)

with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
    for entry in dataset:
        f.write(json.dumps(entry, ensure_ascii=False) + "\n")

print(f"\nDataset saved: {OUTPUT_FILE}")
print(f"Total entries: {len(dataset)}")
print(f"\nTopic distribution:")
for t, c in sorted(topic_counts.items(), key=lambda x: -x[1]):
    print(f"  {t}: {c}")
print(f"\nSample:")
for e in dataset[:2]:
    print(f"  Q: {e['instruction']}")
    print(f"  A: {e['output'][:120]}...")
    print()

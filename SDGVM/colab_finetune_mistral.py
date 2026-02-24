# Mistral-7B LoRA Fine-Tuning для SDGVM (Витте)
# =============================================
# Этот скрипт предназначен для Google Colab (бесплатный T4 GPU).
#
# ИНСТРУКЦИЯ:
# 1. Откройте Google Colab: https://colab.research.google.com
# 2. Создайте новый ноутбук
# 3. Выберите GPU: Runtime → Change runtime type → T4 GPU
# 4. Скопируйте этот код в ячейки и запустите по порядку
#
# Перед запуском загрузите файл witte_dataset.jsonl в Colab
# (через боковую панель Files → Upload)

# ============ ЯЧЕЙКА 1: Установка ============
# !pip install "unsloth[colab-new] @ git+https://github.com/unslothai/unsloth.git"
# !pip install --no-deps trl peft accelerate bitsandbytes xformers

# ============ ЯЧЕЙКА 2: Загрузка модели ============
from unsloth import FastLanguageModel
import torch

model, tokenizer = FastLanguageModel.from_pretrained(
    model_name="unsloth/mistral-7b-instruct-v0.3-bnb-4bit",
    max_seq_length=2048,
    dtype=None,  # Автоопределение (bfloat16 на Ampere+, float16 на T4)
    load_in_4bit=True,
)

print("✅ Модель загружена")

# ============ ЯЧЕЙКА 3: Добавление LoRA ============
model = FastLanguageModel.get_peft_model(
    model,
    r=16,                          # Ранг LoRA
    target_modules=["q_proj", "k_proj", "v_proj", "o_proj",
                     "gate_proj", "up_proj", "down_proj"],
    lora_alpha=16,
    lora_dropout=0,
    bias="none",
    use_gradient_checkpointing="unsloth",
    random_state=42,
)

print("✅ LoRA адаптеры добавлены")
model.print_trainable_parameters()

# ============ ЯЧЕЙКА 4: Загрузка датасета ============
import json
from datasets import Dataset

# Загружаем JSONL файл
data = []
with open("witte_dataset.jsonl", "r", encoding="utf-8") as f:
    for line in f:
        entry = json.loads(line.strip())
        data.append(entry)

print(f"✅ Загружено {len(data)} записей")

# Формат промпта для Mistral Instruct
def format_prompt(example):
    instruction = example.get("instruction", "")
    inp = example.get("input", "")
    output = example.get("output", "")
    
    if inp:
        text = f"""<s>[INST] {instruction}

{inp} [/INST] {output}</s>"""
    else:
        text = f"""<s>[INST] {instruction} [/INST] {output}</s>"""
    
    return {"text": text}

dataset = Dataset.from_list(data)
dataset = dataset.map(format_prompt)

print(f"✅ Датасет подготовлен")
print(f"Пример: {dataset[0]['text'][:300]}...")

# ============ ЯЧЕЙКА 5: Обучение ============
from trl import SFTTrainer
from transformers import TrainingArguments

trainer = SFTTrainer(
    model=model,
    tokenizer=tokenizer,
    train_dataset=dataset,
    dataset_text_field="text",
    max_seq_length=2048,
    dataset_num_proc=2,
    packing=False,
    args=TrainingArguments(
        per_device_train_batch_size=2,
        gradient_accumulation_steps=4,
        warmup_steps=5,
        num_train_epochs=3,           # 3 эпохи
        learning_rate=2e-4,
        fp16=not torch.cuda.is_bf16_supported(),
        bf16=torch.cuda.is_bf16_supported(),
        logging_steps=10,
        optim="adamw_8bit",
        weight_decay=0.01,
        lr_scheduler_type="linear",
        seed=42,
        output_dir="outputs",
        save_strategy="epoch",
    ),
)

print("🚀 Начинаем обучение...")
stats = trainer.train()
print(f"✅ Обучение завершено!")
print(f"   Время: {stats.metrics['train_runtime']:.0f} секунд")
print(f"   Loss: {stats.metrics['train_loss']:.4f}")

# ============ ЯЧЕЙКА 6: Тестирование ============
FastLanguageModel.for_inference(model)

test_prompts = [
    "Расскажи о золотом стандарте Витте.",
    "Как строился Транссиб?",
    "Какие отношения были у Витте с Николаем II?",
]

for prompt in test_prompts:
    inputs = tokenizer(f"<s>[INST] {prompt} [/INST]", return_tensors="pt").to("cuda")
    outputs = model.generate(**inputs, max_new_tokens=256, temperature=0.7)
    response = tokenizer.decode(outputs[0], skip_special_tokens=True)
    print(f"\n❓ {prompt}")
    print(f"💬 {response.split('[/INST]')[-1].strip()[:300]}")

# ============ ЯЧЕЙКА 7: Экспорт в GGUF ============
# GGUF формат нужен для LLMUnity
print("📦 Экспорт в GGUF (Q4_K_M)...")
model.save_pretrained_gguf(
    "mistral-witte",
    tokenizer,
    quantization_method="q4_k_m"  # ~4 ГБ файл
)

print("✅ Файл сохранён: mistral-witte-Q4_K_M.gguf")
print("📥 Скачайте его и положите в папку LLMUnity вашего проекта")

# ============ ЯЧЕЙКА 8: Скачивание ============
# Раскомментируйте для автоматического скачивания:
# from google.colab import files
# files.download("mistral-witte-unsloth.Q4_K_M.gguf")

print("\n🎉 ГОТОВО!")
print("Следующие шаги:")
print("1. Скачайте файл .gguf")
print("2. Положите в папку LLMUnity проекта") 
print("3. В Unity: LLMCharacter → Model → выберите новый файл")

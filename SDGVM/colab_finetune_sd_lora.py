# Stable Diffusion 1.5 LoRA Fine-Tuning (kohya_ss)
# ===============================================
# Инструкция для Google Colab (бесплатный T4 GPU)
# 
# 1. Загрузите папку с аугментированными картинками (Vitte_Photos_Augmented) 
#    в Colab (можно кнопкой Upload Folder на панели слева)
# 2. Создайте ноутбук и выберите T4 GPU (Runtime -> Change runtime type)
# 3. Скопируйте этот скрипт по ячейкам!

# ============ ЯЧЕЙКА 1: Настройка среды ============
# !git clone https://github.com/bmaltais/kohya_ss.git
# %cd kohya_ss
# !chmod +x ./setup.sh
# !./setup.sh -y
# !pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118
# !pip install xformers
# !pip install -U accelerate transformers diffusers

# ============ ЯЧЕЙКА 2: Подготовка папок ============
import os

project_name = "witte_style"
base_dir = "/content/lora_train"
img_dir = f"{base_dir}/img/40_{project_name}" # 40 repeats
log_dir = f"{base_dir}/log"
out_dir = f"{base_dir}/model"

os.makedirs(img_dir, exist_ok=True)
os.makedirs(log_dir, exist_ok=True)
os.makedirs(out_dir, exist_ok=True)

print(f"✅ Папки созданы в {base_dir}")

# ТЕПЕРЬ ПЕРЕМЕСТИТЕ ВАШИ КАРТИНКИ И TXT ФАЙЛЫ (из Vitte_Photos_Augmented) 
# В ПАПКУ: /content/lora_train/img/40_witte_style

# ============ ЯЧЕЙКА 3: Обучение ============
# Когда картинки на месте, запускаем обучение
# %cd /content/kohya_ss

# !accelerate launch --num_cpu_threads_per_process=2 "train_network.py" \
#   --pretrained_model_name_or_path="runwayml/stable-diffusion-v1-5" \
#   --train_data_dir="/content/lora_train/img" \
#   --output_dir="/content/lora_train/model" \
#   --resolution="512,512" \
#   --network_module="networks.lora" \
#   --max_train_epochs=10 \
#   --learning_rate=1e-4 \
#   --network_dim=32 \
#   --network_alpha=16 \
#   --optimizer_type="AdamW8bit" \
#   --mixed_precision="fp16" \
#   --save_every_n_epochs=2 \
#   --save_model_as="safetensors" \
#   --lr_scheduler="constant" \
#   --train_batch_size=2 \
#   --gradient_accumulation_steps=1 \
#   --xformers \
#   --output_name="witte_style_lora"

print("✅ Обучение запущено! (Уберите # у команд выше)")

# ============ ЯЧЕЙКА 4: Скачивание результата ============
# По завершении у вас появится файл: /content/lora_train/model/witte_style_lora.safetensors
# 
# from google.colab import files
# files.download("/content/lora_train/model/witte_style_lora.safetensors")
# 
# 1. Положите этот файл в ComfyUI/models/loras/
# 2. Обновите workflow в SDGVM, добавив ноду LoraLoader 
#    между CheckpointLoader и KSampler (или CLIPTextEncode)

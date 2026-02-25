import os
import cv2
import numpy as np
from pathlib import Path

# ==========================================
# Скрипт для аугментации портретов (увеличение датасета)
# ==========================================
# Что делает:
# берет исходные картинки из INPUT_DIR и генерирует новые варианты в OUTPUT_DIR:
# 1. Оригинал (изменённый до 512x512)
# 2. Отзеркаленный по горизонтали
# 3. С небольшим изменением яркости (+15%)
# 4. С небольшим затемнением (-15%)
# Итого: из 50 картинок получится 200.
# Каждой картинке также создаст пустой текстовый файл для captions, если его нет.

INPUT_DIR = "Vitte_Photos"
OUTPUT_DIR = "Vitte_Photos_Augmented"
TARGET_SIZE = 512 # Оптимально для SD 1.5 LoRA

def adjust_brightness_contrast(image, alpha=1.0, beta=0):
    """
    alpha > 1: больше контраста
    beta > 0: больше яркости
    """
    new_image = cv2.convertScaleAbs(image, alpha=alpha, beta=beta)
    return new_image

def crop_center_and_resize(img, size):
    """Обрезает картинку по центру (квадрат) и ресайзит до size x size"""
    h, w = img.shape[:2]
    min_dim = min(h, w)
    
    start_x = (w - min_dim) // 2
    start_y = (h - min_dim) // 2
    
    cropped = img[start_y:start_y+min_dim, start_x:start_x+min_dim]
    resized = cv2.resize(cropped, (size, size), interpolation=cv2.INTER_AREA)
    return resized

def main():
    if not os.path.exists(INPUT_DIR):
        print(f"❌ Ошибка: Папка {INPUT_DIR} не найдена!")
        return
        
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    
    valid_extensions = {".jpg", ".jpeg", ".png", ".webp"}
    images = [f for f in os.listdir(INPUT_DIR) if Path(f).suffix.lower() in valid_extensions]
    
    if not images:
        print(f"❌ Ошибка: В папке {INPUT_DIR} не найдено картинок!")
        return
        
    print(f"Найдено {len(images)} изображений. Начинаю аугментацию...")
    
    processed_count = 0
    generated_count = 0
    
    for img_name in images:
        img_path = os.path.join(INPUT_DIR, img_name)
        base_name = os.path.splitext(img_name)[0]
        
        # Читаем картинку (cv2.imdecode решает проблему с русскими путями)
        img = cv2.imdecode(np.fromfile(img_path, dtype=np.uint8), cv2.IMREAD_COLOR)
        if img is None:
            print(f"  ⚠️ Не удалось прочитать {img_name}")
            continue
            
        # 0. Подготовка базы (квадрат 512x512)
        base_img = crop_center_and_resize(img, TARGET_SIZE)
        
        # Имя файла для подписей (caption)
        caption = "portrait, digital painting, concept art, highly detailed, sharp focus"
        
        variants = {
            "orig": base_img,
            "flip": cv2.flip(base_img, 1), # Отражение по горизонтали
            "bright": adjust_brightness_contrast(base_img, alpha=1.05, beta=15),
            "dark": adjust_brightness_contrast(base_img, alpha=0.95, beta=-15)
        }
        
        for suffix, variant_img in variants.items():
            new_name = f"{base_name}_{suffix}.jpg"
            out_path = os.path.join(OUTPUT_DIR, new_name)
            
            # Сохраняем картинку
            cv2.imencode('.jpg', variant_img)[1].tofile(out_path)
            
            # Сохраняем файлик с текстом (caption) kohya_ss будет читать его
            txt_path = os.path.join(OUTPUT_DIR, f"{base_name}_{suffix}.txt")
            with open(txt_path, "w", encoding="utf-8") as tf:
                tf.write(caption)
                
            generated_count += 1
            
        processed_count += 1
        print(f"  ✓ {img_name} -> 4 варианта")
        
    print(f"\n✅ Аугментация завершена!")
    print(f"Обработано оригиналов: {processed_count}")
    print(f"Получено новых изображений: {generated_count}")
    print(f"Результат сохранен в папку: {OUTPUT_DIR}")

if __name__ == "__main__":
    main()

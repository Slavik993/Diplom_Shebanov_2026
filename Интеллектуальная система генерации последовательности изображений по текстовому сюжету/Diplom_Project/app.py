import gradio as gr
from core.translator import Translator
from core.text_processing import TextProcessor
from core.generator import ImageGenerator
from utils.storage import Storage
import os

# Инициализация модулей
translator = Translator()
text_processor = TextProcessor()
generator = ImageGenerator()
storage = Storage()

def process_story(story_text, num_scenes, style_prefix, char_desc):
    """Основная функция обработки сюжета."""
    if not story_text:
        return None, "Пожалуйста, введите историю.", [], []

    # 1. Перевод
    print(f"Перевод истории: {story_text[:50]}...")
    translated_story = translator.translate(story_text)
    
    # Translate char_desc if provided
    translated_char_desc = ""
    if char_desc:
        print(f"Перевод описания персонажа: {char_desc}...")
        translated_char_desc = translator.translate(char_desc)

    # 2. Разделение на сцены
    print("Разделение на сцены...")
    scenes = text_processor.split_story_into_scenes(translated_story, int(num_scenes))

    # 3. Генерация изображений
    images = []
    prompts = []
    
    # Инициализация модели если нужно
    if generator.pipeline is None:
        generator.load_model()

    print(f"Генерация {len(scenes)} изображений...")
    for i, scene in enumerate(scenes):
        # Улучшение промпта: добавляем описание персонажа в начало
        full_scene_text = scene
        if translated_char_desc:
            full_scene_text = f"{translated_char_desc}, {scene}"
            
        enhanced_prompt = text_processor.enhance_prompt(full_scene_text, style_prefix)
        prompts.append(enhanced_prompt)
        print(f"Генерация Сцены {i+1}: {enhanced_prompt}")
        
        # Генерация
        img = generator.generate(enhanced_prompt)
        images.append(img)

    # 4. Сохранение результатов
    print("Сохранение результатов...")
    session_dir = storage.save_session(story_text, scenes, prompts, images)
    
    # Форматирование вывода
    gallery_items = []
    for img, prompt in zip(images, prompts):
        gallery_items.append((img, prompt))
        
    status_msg = f"Готово! Сохранено в {session_dir}"
    return gallery_items, status_msg, scenes, prompts

# Определение интерфейса
with gr.Blocks(title="Генератор Визуальных Историй", theme=gr.themes.Soft()) as demo:
    gr.Markdown("# 🎨 Интеллектуальная Система Генерации Изображений")
    gr.Markdown("Введите историю на русском языке, и система создаст последовательность изображений.")

    with gr.Row():
        with gr.Column(scale=1):
            story_input = gr.Textbox(
                label="История (Русский язык)", 
                placeholder="Мальчик гулял по лесу и нашел старый замок...", 
                lines=5
            )
            with gr.Row():
                num_scenes = gr.Slider(minimum=1, maximum=10, value=4, step=1, label="Количество сцен")
                style_prefix = gr.Textbox(
                    label="Стиль (Опционально)", 
                    placeholder="Anime style, Studio Ghibli, Cinematic...", 
                    value="Cinematic, detailed"
                )
            
            char_desc = gr.Textbox(
                label="Главный персонаж / Тема (Опционально)",
                placeholder="Маленький мальчик в красной шапке / Рыжий кот",
                info="Укажите описание главного героя, чтобы он выглядел одинаково на всех картинках."
            )
            
            generate_btn = gr.Button("🚀 Сгенерировать", variant="primary")
            status_output = gr.Textbox(label="Статус", interactive=False)

        with gr.Column(scale=2):
            gallery = gr.Gallery(label="Сгенерированная последовательность", show_label=True, elem_id="gallery", columns=[2], rows=[2], object_fit="contain", height="auto")
            
    with gr.Accordion("Информация для отладки", open=False):
        debug_scenes = gr.JSON(label="Сцены (Английский)")
        debug_prompts = gr.JSON(label="Промпты")

    generate_btn.click(
        fn=process_story,
        inputs=[story_input, num_scenes, style_prefix, char_desc],
        outputs=[gallery, status_output, debug_scenes, debug_prompts]
    )

if __name__ == "__main__":
    demo.launch(share=True)

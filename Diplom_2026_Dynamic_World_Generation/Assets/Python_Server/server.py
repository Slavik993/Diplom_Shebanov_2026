from fastapi import FastAPI
from pydantic import BaseModel
from diffusers import AutoPipelineForText2Image
import uvicorn
import base64
import io

class GenerateRequest(BaseModel):
    prompt: str

# Загрузка простой модели (самая быстрая)
pipe = AutoPipelineForText2Image.from_pretrained(
    "stabilityai/sd-turbo",
    torch_dtype="auto"
)

app = FastAPI()

@app.post("/generate")
def generate(req: GenerateRequest):
    # Генерация изображения
    image = pipe(
        req.prompt,
        num_inference_steps=2
    ).images[0]

    # Кодируем PNG
    buffer = io.BytesIO()
    image.save(buffer, format="PNG")
    encoded = base64.b64encode(buffer.getvalue()).decode()

    return {
        "status": "ok",
        "png_base64": encoded
    }

if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8188)

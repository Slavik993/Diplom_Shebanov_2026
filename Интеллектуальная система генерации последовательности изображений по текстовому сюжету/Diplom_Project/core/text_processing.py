import random

class TextProcessor:
    def __init__(self):
        self.enhancements = [
            "high quality, detailed, digital art",
            "beautiful, cinematic, trending on artstation",
            "sharp focus, studio lighting, ultra detailed",
            "vibrant colors, masterpiece",
            "concept art, 8k resolution"
        ]

    def split_story_into_scenes(self, story_text, num_scenes=4):
        """Splits the story text into a specified number of scenes."""
        # Normalize text: remove extra spaces and split by dots
        story_text = ' '.join(story_text.strip().split())
        sentences = [s.strip() + '.' for s in story_text.split('.') if s.strip()]

        if not sentences:
            return ["Empty scene."] * num_scenes

        # If fewer sentences than scenes, extend
        while len(sentences) < num_scenes:
            sentences.append(sentences[-1])

        # Distribute sentences across scenes
        scenes = []
        # Simple distribution: if we have more sentences than scenes, merge some
        # If we have mostly equal, just map 1 to 1 logic or similar.
        
        # Here we use a simple approach: verify we have at least 'num_scenes' items
        # If we have many sentences, we chunk them.
        
        chunk_size = len(sentences) / num_scenes
        for i in range(num_scenes):
            start = int(i * chunk_size)
            end = int((i + 1) * chunk_size) if i < num_scenes - 1 else len(sentences)
            scene_text = " ".join(sentences[start:end])
            if not scene_text: # Fallback if math goes slightly off or empty chunk
                 scene_text = sentences[min(start, len(sentences)-1)]
            scenes.append(scene_text)

        return scenes

    def enhance_prompt(self, scene_text, style_prefix=""):
        """Enhances the prompt with style keywords."""
        enhancement = random.choice(self.enhancements)
        return f"{style_prefix} {scene_text}, {enhancement}".strip()

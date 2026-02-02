import random

class PromptEngineer:
    def __init__(self):
        self.styles = {
            "Cinematic": "cinematic lighting, dramatic atmosphere, detailed texture, 8k, unreal engine 5 render, ray tracing",
            "Anime": "anime style, makoto shinkai style, studio ghibli, vibrant colors, detailed background, cel shaded",
            "Oil Painting": "oil painting, textured, impressionist, van gogh style, heavy strokes",
            "Cyberpunk": "cyberpunk, neon lights, night city, rain, futuristic, sci-fi, detailed techno",
            "Educational": (
                "clean white background, simple flat illustration, infographic style, clear lines, "
                "minimalist diagram, vector art, high contrast, legible, educational diagram, textbook style, "
                "no shadows, no gradients, schematic, explanatory illustration, 4k resolution"
            ),
            "Scheme": (
                "technical scheme, blueprint style, black and white line art, precise, labeled diagram, "
                "flowchart elements, arrows, boxes, clear text placeholders, engineering drawing"
            ),
            "Presentation Slide": (
                "powerpoint slide style, clean layout, large text placeholders, minimalistic, corporate design, "
                "white background, blue accents, professional, high readability"
            ),
            "Algorithm Flowchart": (
                "flowchart diagram, algorithm visualization, decision boxes, process steps, arrows, "
                "data flow, pseudocode elements, clean technical illustration, programming logic diagram"
            ),
            "Database Schema": (
                "database diagram, entity relationship model, tables, relationships, primary keys, "
                "foreign keys, crow's foot notation, clean database design, technical schema"
            ),
            "Neural Network": (
                "neural network architecture, layers visualization, nodes, connections, data flow, "
                "machine learning diagram, AI model structure, clean technical illustration"
            ),
            "Web Interface": (
                "web design mockup, user interface layout, wireframe, buttons, forms, navigation, "
                "responsive design, clean UI/UX illustration, website structure"
            ),
            "Code Structure": (
                "code architecture diagram, class hierarchy, modules, dependencies, clean code visualization, "
                "software design patterns, object oriented design, technical code diagram"
            )
        }
        self.camera_angles = [
            "wide angle shot", "close up", "eye level", "low angle", "hero shot", "panoramic view"
        ]
        self.lighting = [
            "natural lighting", "studio lighting", "soft creative lighting", "volumetric lighting", "rembrandt lighting"
        ]

    def build_prompt(self, base_description, style_name="Cinematic", character_desc="", add_random_camera=False, educational_mode=False):
        """
        Constructs a complex prompt based on multiple parameters.
        """
        components = []

        # 1. Base Logic: Subject (Character + Action)
        if character_desc:
            components.append(f"{character_desc}, {base_description}")
        else:
            components.append(base_description)

        # 2. Style Injection
        style_prompt = self.styles.get(style_name, self.styles["Cinematic"])
        components.append(style_prompt)

        # 3. Educational enhancements
        if educational_mode:
            components.append("simple illustration:1.3, clean background:1.4, high contrast:1.2, legible text:1.3")

        # 4. Camera (Optional)
        if add_random_camera:
            components.append(random.choice(self.camera_angles))

        # 5. Lighting (Randomized for variety but keeping high quality)
        if not educational_mode:  # Skip lighting for educational to keep clean
            components.append(random.choice(self.lighting))

        # 6. Quality Boosters
        components.append("high quality, masterpiece, sharp focus")

        return ", ".join(components)

    def get_available_styles(self):
        return list(self.styles.keys())

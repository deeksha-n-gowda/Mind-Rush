from flask_pymongo import PyMongo
from bson import ObjectId


class Character:
    """Character model for unlockable avatars with perks."""

    collection_name = "characters"

    # Static character definitions (could be moved to DB for live ops)
    CHARACTER_DEFINITIONS = {
        "aura": {
            "id": "aura",
            "name": "Aura",
            "description": "The balanced starter focus avatar.",
            "rarity": "common",
            "perk": {
                "type": "balanced",
                "description": "Balanced stats — good for learning the ropes."
            },
            "unlock_requirement": {
                "type": "default",
                "description": "Available from the start"
            },
            "visual": {
                "model": "aura",
                "color": "#FFD700",
                "trail_effect": "soft_glow"
            }
        },
        "dr_zen": {
            "id": "dr_zen",
            "name": "Dr. Zen",
            "description": "Calm mind slows the chaos.",
            "rarity": "rare",
            "perk": {
                "type": "slow_distractions",
                "value": 0.10,
                "description": "Slows distraction pop-ups by 10%"
            },
            "unlock_requirement": {
                "type": "level",
                "value": 5,
                "description": "Reach Level 5"
            },
            "visual": {
                "model": "dr_zen",
                "color": "#4ECDC4",
                "trail_effect": "calm_ripple"
            }
        },
        "pixel": {
            "id": "pixel",
            "name": "Pixel",
            "description": "Retro focus, double the points.",
            "rarity": "epic",
            "perk": {
                "type": "score_multiplier",
                "value": 2.0,
                "description": "2x Score Combo Boost"
            },
            "unlock_requirement": {
                "type": "collect_ideas",
                "value": 1000,
                "description": "Collect 1,000 Idea Lightbulbs"
            },
            "visual": {
                "model": "pixel",
                "color": "#FF6B6B",
                "trail_effect": "pixel_particles"
            }
        },
        "pulse": {
            "id": "pulse",
            "name": "Pulse",
            "description": "Quick reflexes, faster lanes.",
            "rarity": "epic",
            "perk": {
                "type": "lane_speed",
                "value": 0.20,
                "description": "20% faster lane-switching speed"
            },
            "unlock_requirement": {
                "type": "level",
                "value": 15,
                "description": "Reach Level 15"
            },
            "visual": {
                "model": "pulse",
                "color": "#00D2FF",
                "trail_effect": "speed_lines"
            }
        },
        "luna": {
            "id": "luna",
            "name": "Luna",
            "description": "Night owl with high-contrast focus.",
            "rarity": "legendary",
            "perk": {
                "type": "night_vision",
                "description": "High-contrast night focus glow — see obstacles earlier in dark segments"
            },
            "unlock_requirement": {
                "type": "daily_challenges",
                "value": 10,
                "description": "Complete 10 Daily Challenges"
            },
            "visual": {
                "model": "luna",
                "color": "#B19CD9",
                "trail_effect": "moonlight_glow"
            }
        }
    }

    def __init__(self, mongo: PyMongo):
        self.mongo = mongo
        self.collection = mongo.db[self.collection_name]

    def get_all(self) -> list:
        """Return all character definitions."""
        return list(self.CHARACTER_DEFINITIONS.values())

    def get_by_id(self, character_id: str) -> dict | None:
        return self.CHARACTER_DEFINITIONS.get(character_id)

    def check_unlock_requirement(self, character_id: str, player_doc: dict) -> tuple[bool, str]:
        """Check if player meets unlock requirement for a character."""
        char_def = self.get_by_id(character_id)
        if not char_def:
            return False, "Character not found"

        req = char_def["unlock_requirement"]
        req_type = req["type"]

        if req_type == "default":
            return True, "Available by default"

        elif req_type == "level":
            if player_doc.get("level", 1) >= req["value"]:
                return True, f"Level {req['value']} reached"
            return False, f"Requires Level {req['value']} (you are Level {player_doc.get('level', 1)})"

        elif req_type == "collect_ideas":
            if player_doc.get("total_ideas_collected", 0) >= req["value"]:
                return True, f"{req['value']} ideas collected"
            return False, f"Requires {req['value']} ideas (you have {player_doc.get('total_ideas_collected', 0)})"

        elif req_type == "daily_challenges":
            # This would need a separate daily challenge completion count
            completed = player_doc.get("daily_challenges_completed", 0)
            if completed >= req["value"]:
                return True, f"{req['value']} daily challenges completed"
            return False, f"Requires {req['value']} daily challenges (you have {completed})"

        return False, "Unknown requirement"

    def get_perk_for_character(self, character_id: str) -> dict | None:
        """Get the perk data for a character."""
        char_def = self.get_by_id(character_id)
        if char_def:
            return char_def.get("perk")
        return None

    def apply_perk(self, character_id: str, base_value: float) -> float:
        """Apply character perk to a base value (e.g., lane speed, distraction timer)."""
        perk = self.get_perk_for_character(character_id)
        if not perk:
            return base_value

        perk_type = perk.get("type")
        value = perk.get("value", 0)

        if perk_type == "lane_speed":
            return base_value * (1 + value)  # 20% faster
        elif perk_type == "slow_distractions":
            return base_value * (1 - value)  # 10% slower
        elif perk_type == "score_multiplier":
            return base_value * value  # 2x

        return base_value
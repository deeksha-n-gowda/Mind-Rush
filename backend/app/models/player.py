from datetime import datetime
from flask_pymongo import PyMongo
from bson import ObjectId
from werkzeug.security import generate_password_hash, check_password_hash


class Player:
    """Player model for authentication and profile data."""

    collection_name = "players"

    def __init__(self, mongo: PyMongo):
        self.mongo = mongo
        self.collection = mongo.db[self.collection_name]

    def create(self, username: str, email: str, password: str) -> dict:
        """Create a new player account."""
        now = datetime.utcnow()
        player_doc = {
            "username": username,
            "email": email,
            "password_hash": generate_password_hash(password),
            "created_at": now,
            "updated_at": now,
            "level": 1,
            "experience": 0,
            "total_ideas_collected": 0,
            "total_gems_collected": 0,
            "high_score": 0,
            "unlocked_characters": ["aura"],
            "equipped_character": "aura",
            "daily_streak": 0,
            "last_daily_date": None,
            "statistics": {
                "total_runs": 0,
                "total_distance": 0,
                "total_playtime_seconds": 0,
                "deaths_by_obstacle": {}
            }
        }
        result = self.collection.insert_one(player_doc)
        player_doc["_id"] = result.inserted_id
        return player_doc

    def find_by_username(self, username: str) -> dict | None:
        return self.collection.find_one({"username": username})

    def find_by_email(self, email: str) -> dict | None:
        return self.collection.find_one({"email": email})

    def find_by_id(self, player_id: str) -> dict | None:
        return self.collection.find_one({"_id": ObjectId(player_id)})

    def verify_password(self, player_doc: dict, password: str) -> bool:
        return check_password_hash(player_doc["password_hash"], password)

    def update_profile(self, player_id: str, updates: dict) -> bool:
        updates["updated_at"] = datetime.utcnow()
        result = self.collection.update_one(
            {"_id": ObjectId(player_id)},
            {"$set": updates}
        )
        return result.modified_count > 0

    def add_experience(self, player_id: str, xp: int) -> dict | None:
        """Add XP and handle level up."""
        player = self.find_by_id(player_id)
        if not player:
            return None

        new_xp = player["experience"] + xp
        new_level = player["level"]
        # Simple level formula: 100 * level^2 XP needed
        while new_xp >= 100 * (new_level ** 2):
            new_xp -= 100 * (new_level ** 2)
            new_level += 1

        self.collection.update_one(
            {"_id": ObjectId(player_id)},
            {"$set": {"experience": new_xp, "level": new_level, "updated_at": datetime.utcnow()}}
        )
        return self.find_by_id(player_id)

    def unlock_character(self, player_id: str, character_id: str) -> bool:
        result = self.collection.update_one(
            {"_id": ObjectId(player_id)},
            {"$addToSet": {"unlocked_characters": character_id}, "$set": {"updated_at": datetime.utcnow()}}
        )
        return result.modified_count > 0

    def equip_character(self, player_id: str, character_id: str) -> bool:
        # Verify they own it
        player = self.find_by_id(player_id)
        if not player or character_id not in player.get("unlocked_characters", []):
            return False

        result = self.collection.update_one(
            {"_id": ObjectId(player_id)},
            {"$set": {"equipped_character": character_id, "updated_at": datetime.utcnow()}}
        )
        return result.modified_count > 0

    def update_high_score(self, player_id: str, score: int) -> bool:
        player = self.find_by_id(player_id)
        if not player or score <= player.get("high_score", 0):
            return False

        self.collection.update_one(
            {"_id": ObjectId(player_id)},
            {"$set": {"high_score": score, "updated_at": datetime.utcnow()}}
        )
        return True

    def increment_statistics(self, player_id: str, updates: dict) -> bool:
        """Increment statistical counters."""
        set_updates = {"updated_at": datetime.utcnow()}
        inc_updates = {}
        for key, value in updates.items():
            if key.startswith("statistics."):
                inc_updates[key] = value
            else:
                set_updates[key] = value

        update_doc = {}
        if set_updates:
            update_doc["$set"] = set_updates
        if inc_updates:
            update_doc["$inc"] = inc_updates

        if not update_doc:
            return False

        result = self.collection.update_one({"_id": ObjectId(player_id)}, update_doc)
        return result.modified_count > 0

    def update_daily_streak(self, player_id: str) -> dict | None:
        """Update daily login streak."""
        player = self.find_by_id(player_id)
        if not player:
            return None

        today = datetime.utcnow().date()
        last_daily = player.get("last_daily_date")

        if last_daily and last_daily.date() == today:
            return player  # Already claimed today

        new_streak = 1
        if last_daily and (today - last_daily.date()).days == 1:
            new_streak = player.get("daily_streak", 0) + 1

        self.collection.update_one(
            {"_id": ObjectId(player_id)},
            {"$set": {"daily_streak": new_streak, "last_daily_date": datetime.utcnow(), "updated_at": datetime.utcnow()}}
        )
        return self.find_by_id(player_id)

    def to_public_dict(self, player_doc: dict) -> dict:
        """Convert to dict safe for JSON response."""
        return {
            "id": str(player_doc["_id"]),
            "username": player_doc["username"],
            "email": player_doc["email"],
            "level": player_doc["level"],
            "experience": player_doc["experience"],
            "total_ideas_collected": player_doc.get("total_ideas_collected", 0),
            "total_gems_collected": player_doc.get("total_gems_collected", 0),
            "high_score": player_doc.get("high_score", 0),
            "unlocked_characters": player_doc.get("unlocked_characters", ["aura"]),
            "equipped_character": player_doc.get("equipped_character", "aura"),
            "daily_streak": player_doc.get("daily_streak", 0),
            "statistics": player_doc.get("statistics", {})
        }
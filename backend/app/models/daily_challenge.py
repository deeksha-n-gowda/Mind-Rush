from datetime import datetime, timedelta
from flask_pymongo import PyMongo
from bson import ObjectId
import random


class DailyChallenge:
    """Daily Challenge model for retention mechanics."""

    collection_name = "daily_challenges"
    player_progress_collection = "player_daily_progress"

    CHALLENGE_TYPES = [
        {"type": "collect_ideas", "target": 50, "reward_xp": 200, "description": "Collect 50 Idea Lightbulbs"},
        {"type": "collect_gems", "target": 10, "reward_xp": 300, "description": "Collect 10 Focus Gems"},
        {"type": "reach_distance", "target": 5000, "reward_xp": 250, "description": "Run 5,000m in a single run"},
        {"type": "survive_time", "target": 120, "reward_xp": 200, "description": "Survive for 2 minutes"},
        {"type": "avoid_obstacles", "target": 30, "reward_xp": 150, "description": "Dodge 30 obstacles without hitting any"},
        {"type": "perfect_lane_changes", "target": 20, "reward_xp": 180, "description": "Make 20 perfect lane changes"},
        {"type": "high_score", "target": 10000, "reward_xp": 300, "description": "Score 10,000+ points in one run"},
    ]

    def __init__(self, mongo: PyMongo):
        self.mongo = mongo
        self.challenges = mongo.db[self.collection_name]
        self.progress = mongo.db[self.player_progress_collection]

    def get_or_create_today(self) -> dict:
        """Get today's challenge, creating if needed."""
        today = datetime.utcnow().replace(hour=0, minute=0, second=0, microsecond=0)

        challenge = self.challenges.find_one({"date": today})
        if challenge:
            return challenge

        # Pick 3 random challenges for today
        selected = random.sample(self.CHALLENGE_TYPES, 3)
        challenge_doc = {
            "date": today,
            "challenges": selected,
            "created_at": datetime.utcnow()
        }
        result = self.challenges.insert_one(challenge_doc)
        challenge_doc["_id"] = result.inserted_id
        return challenge_doc

    def get_player_progress(self, player_id: str, date: datetime = None) -> dict:
        """Get player's progress on today's challenges."""
        if date is None:
            date = datetime.utcnow().replace(hour=0, minute=0, second=0, microsecond=0)

        prog = self.progress.find_one({
            "player_id": ObjectId(player_id),
            "date": date
        })
        if not prog:
            return {
                "player_id": ObjectId(player_id),
                "date": date,
                "progress": {str(i): 0 for i in range(3)},
                "completed": [False, False, False],
                "claimed": [False, False, False],
                "updated_at": datetime.utcnow()
            }
        return prog

    def update_progress(self, player_id: str, challenge_index: int, increment: int = 1) -> dict:
        """Increment progress for a specific challenge."""
        today = datetime.utcnow().replace(hour=0, minute=0, second=0, microsecond=0)
        challenge = self.get_or_create_today()

        if challenge_index >= len(challenge["challenges"]):
            return {"error": "Invalid challenge index"}

        target = challenge["challenges"][challenge_index]["target"]
        prog = self.get_player_progress(player_id, today)

        current = prog["progress"].get(str(challenge_index), 0)
        new_value = min(current + increment, target)
        prog["progress"][str(challenge_index)] = new_value
        prog["updated_at"] = datetime.utcnow()

        # Check completion
        if new_value >= target and not prog["completed"][challenge_index]:
            prog["completed"][challenge_index] = True

        self.progress.update_one(
            {"player_id": ObjectId(player_id), "date": today},
            {"$set": prog},
            upsert=True
        )
        return prog

    def claim_reward(self, player_id: str, challenge_index: int) -> dict | None:
        """Claim XP reward for completed challenge."""
        today = datetime.utcnow().replace(hour=0, minute=0, second=0, microsecond=0)
        prog = self.get_player_progress(player_id, today)

        if not prog["completed"][challenge_index]:
            return {"error": "Challenge not completed"}

        if prog["claimed"][challenge_index]:
            return {"error": "Already claimed"}

        challenge = self.get_or_create_today()
        reward_xp = challenge["challenges"][challenge_index]["reward_xp"]

        prog["claimed"][challenge_index] = True
        prog["updated_at"] = datetime.utcnow()

        self.progress.update_one(
            {"player_id": ObjectId(player_id), "date": today},
            {"$set": prog}
        )

        return {"reward_xp": reward_xp, "challenge_index": challenge_index}

    def get_all_progress(self, player_id: str) -> dict:
        """Get full progress for today's challenges with metadata."""
        today = datetime.utcnow().replace(hour=0, minute=0, second=0, microsecond=0)
        challenge = self.get_or_create_today()
        prog = self.get_player_progress(player_id, today)

        result = {
            "date": today.isoformat(),
            "challenges": []
        }

        for i, ch in enumerate(challenge["challenges"]):
            current = prog["progress"].get(str(i), 0)
            target = ch["target"]
            result["challenges"].append({
                "index": i,
                "type": ch["type"],
                "description": ch["description"],
                "target": target,
                "current": current,
                "completed": prog["completed"][i],
                "claimed": prog["claimed"][i],
                "reward_xp": ch["reward_xp"]
            })

        return result
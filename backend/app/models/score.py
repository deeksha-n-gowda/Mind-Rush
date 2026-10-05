from datetime import datetime
from flask_pymongo import PyMongo
from bson import ObjectId


class Score:
    """Score model for individual run records."""

    collection_name = "scores"

    def __init__(self, mongo: PyMongo):
        self.mongo = mongo
        self.collection = mongo.db[self.collection_name]

    def create(self, player_id: str, score: int, distance: int, ideas_collected: int,
               gems_collected: int, playtime_seconds: int, character_used: str,
               cause_of_death: str = None) -> dict:
        """Record a completed run."""
        now = datetime.utcnow()
        score_doc = {
            "player_id": ObjectId(player_id),
            "score": score,
            "distance": distance,
            "ideas_collected": ideas_collected,
            "gems_collected": gems_collected,
            "playtime_seconds": playtime_seconds,
            "character_used": character_used,
            "cause_of_death": cause_of_death,
            "created_at": now
        }
        result = self.collection.insert_one(score_doc)
        score_doc["_id"] = result.inserted_id
        return score_doc

    def find_by_player(self, player_id: str, limit: int = 20, skip: int = 0) -> list:
        """Get recent scores for a player."""
        return list(self.collection.find(
            {"player_id": ObjectId(player_id)}
        ).sort("created_at", -1).skip(skip).limit(limit))

    def get_best_score(self, player_id: str) -> dict | None:
        """Get player's highest score."""
        return self.collection.find_one(
            {"player_id": ObjectId(player_id)},
            sort=[("score", -1)]
        )

    def get_leaderboard(self, limit: int = 50, skip: int = 0) -> list:
        """Get global leaderboard (top scores)."""
        pipeline = [
            {"$sort": {"score": -1}},
            {"$group": {
                "_id": "$player_id",
                "best_score": {"$first": "$$ROOT"}
            }},
            {"$replaceRoot": {"newRoot": "$best_score"}},
            {"$sort": {"score": -1}},
            {"$skip": skip},
            {"$limit": limit}
        ]
        return list(self.collection.aggregate(pipeline))

    def get_leaderboard_with_players(self, limit: int = 50, skip: int = 0) -> list:
        """Get leaderboard with player usernames."""
        pipeline = [
            {"$sort": {"score": -1}},
            {"$group": {
                "_id": "$player_id",
                "best_score": {"$first": "$$ROOT"}
            }},
            {"$replaceRoot": {"newRoot": "$best_score"}},
            {"$sort": {"score": -1}},
            {"$skip": skip},
            {"$limit": limit},
            {"$lookup": {
                "from": "players",
                "localField": "player_id",
                "foreignField": "_id",
                "as": "player"
            }},
            {"$unwind": "$player"},
            {"$project": {
                "score": 1,
                "distance": 1,
                "ideas_collected": 1,
                "gems_collected": 1,
                "playtime_seconds": 1,
                "character_used": 1,
                "created_at": 1,
                "username": "$player.username",
                "equipped_character": "$player.equipped_character"
            }}
        ]
        return list(self.collection.aggregate(pipeline))

    def get_daily_leaderboard(self, date: datetime, limit: int = 20) -> list:
        """Get leaderboard for a specific day."""
        start = datetime(date.year, date.month, date.day)
        end = datetime(date.year, date.month, date.day, 23, 59, 59)

        pipeline = [
            {"$match": {"created_at": {"$gte": start, "$lte": end}}},
            {"$sort": {"score": -1}},
            {"$group": {
                "_id": "$player_id",
                "best_score": {"$first": "$$ROOT"}
            }},
            {"$replaceRoot": {"newRoot": "$best_score"}},
            {"$sort": {"score": -1}},
            {"$limit": limit},
            {"$lookup": {
                "from": "players",
                "localField": "player_id",
                "foreignField": "_id",
                "as": "player"
            }},
            {"$unwind": "$player"},
            {"$project": {
                "score": 1,
                "distance": 1,
                "created_at": 1,
                "username": "$player.username"
            }}
        ]
        return list(self.collection.aggregate(pipeline))

    def to_public_dict(self, score_doc: dict) -> dict:
        return {
            "id": str(score_doc["_id"]),
            "player_id": str(score_doc["player_id"]),
            "score": score_doc["score"],
            "distance": score_doc["distance"],
            "ideas_collected": score_doc["ideas_collected"],
            "gems_collected": score_doc["gems_collected"],
            "playtime_seconds": score_doc["playtime_seconds"],
            "character_used": score_doc["character_used"],
            "cause_of_death": score_doc.get("cause_of_death"),
            "created_at": score_doc["created_at"].isoformat() if score_doc.get("created_at") else None
        }
from flask import Blueprint, request, jsonify, current_app
from app.models import Score, Character

bp = Blueprint("leaderboard", __name__)

def get_score_model():
    return Score(current_app.mongo)

def get_character_model():
    return Character(current_app.mongo)

@bp.route("", methods=["GET"])
def get_leaderboard():
    """Global leaderboard with player info."""
    limit = min(int(request.args.get("limit", 50)), 100)
    skip = int(request.args.get("skip", 0))

    score_model = get_score_model()
    entries = score_model.get_leaderboard_with_players(limit, skip)

    return jsonify({
        "leaderboard": [
            {
                "rank": skip + i + 1,
                "username": e["username"],
                "score": e["score"],
                "distance": e["distance"],
                "ideas_collected": e["ideas_collected"],
                "gems_collected": e["gems_collected"],
                "character_used": e["character_used"],
                "equipped_character": e.get("equipped_character"),
                "created_at": e["created_at"].isoformat() if e.get("created_at") else None
            }
            for i, e in enumerate(entries)
        ]
    }), 200

@bp.route("/daily", methods=["GET"])
def get_daily_leaderboard():
    """Leaderboard for today."""
    from datetime import datetime
    limit = min(int(request.args.get("limit", 20)), 50)

    score_model = get_score_model()
    today = datetime.utcnow().replace(hour=0, minute=0, second=0, microsecond=0)
    entries = score_model.get_daily_leaderboard(today, limit)

    return jsonify({
        "date": today.isoformat(),
        "leaderboard": [
            {
                "rank": i + 1,
                "username": e["username"],
                "score": e["score"],
                "distance": e["distance"],
                "created_at": e["created_at"].isoformat() if e.get("created_at") else None
            }
            for i, e in enumerate(entries)
        ]
    }), 200

@bp.route("/character/<character_id>", methods=["GET"])
def get_character_leaderboard(character_id: str):
    """Leaderboard filtered by character used."""
    char_model = get_character_model()
    if not char_model.get_by_id(character_id):
        return jsonify({"error": "Character not found"}), 404

    limit = min(int(request.args.get("limit", 20)), 50)
    skip = int(request.args.get("skip", 0))

    score_model = get_score_model()
    pipeline = [
        {"$match": {"character_used": character_id}},
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
            "created_at": 1,
            "username": "$player.username"
        }}
    ]
    entries = list(score_model.collection.aggregate(pipeline))

    return jsonify({
        "character": character_id,
        "leaderboard": [
            {
                "rank": skip + i + 1,
                "username": e["username"],
                "score": e["score"],
                "distance": e["distance"],
                "ideas_collected": e["ideas_collected"],
                "gems_collected": e["gems_collected"],
                "created_at": e["created_at"].isoformat() if e.get("created_at") else None
            }
            for i, e in enumerate(entries)
        ]
    }), 200
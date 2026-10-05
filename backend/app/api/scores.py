from flask import Blueprint, request, jsonify, current_app
from app.models import Score, Player

bp = Blueprint("scores", __name__)

def get_score_model():
    return Score(current_app.mongo)

def get_player_model():
    return Player(current_app.mongo)

def get_current_player_id() -> str | None:
    auth_header = request.headers.get("Authorization", "")
    if not auth_header.startswith("Bearer "):
        return None
    import jwt
    try:
        payload = jwt.decode(
            auth_header[7:],
            current_app.config["JWT_SECRET"],
            algorithms=[current_app.config["JWT_ALGORITHM"]]
        )
        return payload.get("sub")
    except jwt.InvalidTokenError:
        return None

@bp.route("", methods=["POST"])
def submit_score():
    player_id = get_current_player_id()
    if not player_id:
        return jsonify({"error": "Unauthorized"}), 401

    data = request.get_json() or {}
    try:
        score = int(data.get("score", 0))
        distance = int(data.get("distance", 0))
        ideas_collected = int(data.get("ideas_collected", 0))
        gems_collected = int(data.get("gems_collected", 0))
        playtime_seconds = int(data.get("playtime_seconds", 0))
        character_used = data.get("character_used", "aura")
        cause_of_death = data.get("cause_of_death")
    except (ValueError, TypeError):
        return jsonify({"error": "Invalid score data"}), 400

    if score < 0 or distance < 0:
        return jsonify({"error": "Invalid values"}), 400

    score_model = get_score_model()
    player_model = get_player_model()

    score_doc = score_model.create(
        player_id, score, distance, ideas_collected,
        gems_collected, playtime_seconds, character_used, cause_of_death
    )

    # Update player stats
    player_model.update_high_score(player_id, score)
    player_model.increment_statistics(player_id, {
        "statistics.total_runs": 1,
        "statistics.total_distance": distance,
        "statistics.total_playtime_seconds": playtime_seconds,
        f"statistics.deaths_by_obstacle.{cause_of_death or 'unknown'}": 1
    })
    player_model.add_experience(player_id, score // 10)  # 1 XP per 10 score
    player_model.increment_statistics(player_id, {
        "total_ideas_collected": ideas_collected,
        "total_gems_collected": gems_collected
    })

    return jsonify({
        "message": "Score submitted",
        "score": score_model.to_public_dict(score_doc)
    }), 201

@bp.route("/my", methods=["GET"])
def my_scores():
    player_id = get_current_player_id()
    if not player_id:
        return jsonify({"error": "Unauthorized"}), 401

    limit = min(int(request.args.get("limit", 20)), 100)
    skip = int(request.args.get("skip", 0))

    score_model = get_score_model()
    scores = score_model.find_by_player(player_id, limit, skip)

    return jsonify({
        "scores": [score_model.to_public_dict(s) for s in scores]
    }), 200

@bp.route("/best", methods=["GET"])
def best_score():
    player_id = get_current_player_id()
    if not player_id:
        return jsonify({"error": "Unauthorized"}), 401

    score_model = get_score_model()
    best = score_model.get_best_score(player_id)

    if not best:
        return jsonify({"score": None}), 200

    return jsonify({"score": score_model.to_public_dict(best)}), 200
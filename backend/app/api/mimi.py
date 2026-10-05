from flask import Blueprint, request, jsonify, current_app
from app.models import DailyChallenge, Player

bp = Blueprint("mimi", __name__)

# Mimi's personality responses
MIMI_RESPONSES = {
    "milestone": [
        "Wow, {distance}m! Your focus is expanding!",
        "Incredible! {score} points — the mind is clearing!",
        "Another bright idea collected! Total: {ideas}. Keep going!",
        "Focus gem #{gems} secured. Clarity increasing...",
    ],
    "near_miss": [
        "Close one! That notification nearly got you.",
        "Phew! Dodged that email avalanche by a thread.",
        "Watch the alarm clocks — they're sneaky!",
        "Stay sharp. Distractions are multiplying.",
    ],
    "death": [
        "Don't worry — every thought has its end. Ready for a fresh start?",
        "The mind resets. What will you focus on next time?",
        "A moment of chaos. Next run, find your calm.",
        "Even the brightest minds get distracted. Run again?",
    ],
    "unlock": [
        "New ally joined! Welcome, {character_name}!",
        "{character_name} brings a fresh perspective. Try them out!",
        "Your inner circle grows. {character_name} is ready!",
    ],
    "daily_complete": [
        "Daily challenge complete! Your discipline shines.",
        "Another day, another victory for focus. +{xp} XP!",
        "Consistency builds clarity. Well done!",
    ],
    "level_up": [
        "Level {level} reached! Your mind expands.",
        "Growing stronger... Level {level} unlocked!",
        "New level, new possibilities. Welcome to Level {level}!",
    ],
    "idle": [
        "The mind wanders... ready to focus?",
        "Thoughts drifting. Shall we begin?",
        "Quiet moment. Perfect for a run.",
    ]
}

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

import random

@bp.route("/commentary", methods=["POST"])
def get_commentary():
    """
    Body: {
        "event": "milestone|near_miss|death|unlock|daily_complete|level_up|idle",
        "context": {
            "distance": 1500,
            "score": 5000,
            "ideas": 12,
            "gems": 3,
            "character_name": "Dr. Zen",
            "xp": 200,
            "level": 5
        }
    }
    Returns: { "text": "Mimi's commentary", "audio_url": null }
    """
    data = request.get_json() or {}
    event = data.get("event", "idle")
    context = data.get("context", {})

    responses = MIMI_RESPONSES.get(event, MIMI_RESPONSES["idle"])
    template = random.choice(responses)

    # Format with context
    try:
        text = template.format(**context)
    except KeyError:
        text = template

    # TODO: Generate actual TTS audio here (ElevenLabs, Azure, etc.)
    # For now return text only
    return jsonify({
        "text": text,
        "audio_url": None,
        "event": event
    }), 200

@bp.route("/personality", methods=["GET"])
def get_personality():
    """Get Mimi's personality info for UI."""
    return jsonify({
        "name": "Mimi",
        "title": "Inner Voice Guide",
        "description": "Your companion through the mindscape. Offers encouragement, warnings, and celebrations.",
        "voice_style": "calm, encouraging, slightly playful",
        "events_supported": list(MIMI_RESPONSES.keys())
    }), 200

@bp.route("/daily-challenges", methods=["GET"])
def get_daily_challenges():
    player_id = get_current_player_id()
    if not player_id:
        return jsonify({"error": "Unauthorized"}), 401

    challenge_model = DailyChallenge(current_app.mongo)
    progress = challenge_model.get_all_progress(player_id)

    return jsonify(progress), 200

@bp.route("/daily-challenges/<int:challenge_index>/claim", methods=["POST"])
def claim_daily_challenge(challenge_index: int):
    player_id = get_current_player_id()
    if not player_id:
        return jsonify({"error": "Unauthorized"}), 401

    challenge_model = DailyChallenge(current_app.mongo)
    result = challenge_model.claim_reward(player_id, challenge_index)

    if "error" in result:
        return jsonify(result), 400

    # Award XP to player
    player_model = Player(current_app.mongo)
    player_model.add_experience(player_id, result["reward_xp"])

    return jsonify({
        "message": "Reward claimed!",
        "reward_xp": result["reward_xp"]
    }), 200
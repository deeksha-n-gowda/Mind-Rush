from flask import Blueprint, request, jsonify

bp = Blueprint("mimi", __name__)

@bp.route("/commentary", methods=["POST"])
def get_commentary():
    """
    Body: {
        "event": "milestone|near_miss|death|unlock|daily_complete",
        "context": { ... }
    }
    Returns: { "text": "Mimi's commentary", "audio_url": "..." }
    """
    data = request.get_json()
    event = data.get("event") if data else None
    # TODO: Call TTS service, generate contextual response
    return jsonify({
        "text": f"Mimi commentary for {event} - TODO",
        "audio_url": None
    }), 501
from flask import Blueprint, request, jsonify, current_app
from app.models import Character, Player
from app import mongo

bp = Blueprint("characters", __name__)

def get_character_model():
    return Character(mongo)

def get_player_model():
    return Player(mongo)

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

@bp.route("", methods=["GET"])
def list_characters():
    """Public endpoint - no auth required to see character list."""
    char_model = get_character_model()
    characters = char_model.get_all()

    # If authenticated, add unlock status
    player_id = get_current_player_id()
    if player_id:
        player_model = get_player_model()
        player_doc = player_model.find_by_id(player_id)
        if player_doc:
            unlocked = set(player_doc.get("unlocked_characters", ["aura"]))
            for char in characters:
                char["unlocked"] = char["id"] in unlocked
                can_unlock, reason = char_model.check_unlock_requirement(char["id"], player_doc)
                char["can_unlock"] = can_unlock
                char["unlock_reason"] = reason
        else:
            for char in characters:
                char["unlocked"] = char["id"] == "aura"
                char["can_unlock"] = False
                char["unlock_reason"] = "Login to check"
    else:
        for char in characters:
            char["unlocked"] = char["id"] == "aura"
            char["can_unlock"] = False
            char["unlock_reason"] = "Login to check"

    return jsonify({"characters": characters}), 200

@bp.route("/<character_id>", methods=["GET"])
def get_character(character_id: str):
    char_model = get_character_model()
    char = char_model.get_by_id(character_id)
    if not char:
        return jsonify({"error": "Character not found"}), 404

    player_id = get_current_player_id()
    if player_id:
        player_model = get_player_model()
        player_doc = player_model.find_by_id(player_id)
        if player_doc:
            unlocked = set(player_doc.get("unlocked_characters", ["aura"]))
            char["unlocked"] = char["id"] in unlocked
            can_unlock, reason = char_model.check_unlock_requirement(char["id"], player_doc)
            char["can_unlock"] = can_unlock
            char["unlock_reason"] = reason

    return jsonify({"character": char}), 200

@bp.route("/<character_id>/unlock", methods=["POST"])
def unlock_character(character_id: str):
    player_id = get_current_player_id()
    if not player_id:
        return jsonify({"error": "Unauthorized"}), 401

    char_model = get_character_model()
    player_model = get_player_model()

    player_doc = player_model.find_by_id(player_id)
    if not player_doc:
        return jsonify({"error": "Player not found"}), 404

    if character_id in player_doc.get("unlocked_characters", ["aura"]):
        return jsonify({"error": "Already unlocked"}), 400

    can_unlock, reason = char_model.check_unlock_requirement(character_id, player_doc)
    if not can_unlock:
        return jsonify({"error": f"Cannot unlock: {reason}"}), 400

    if player_model.unlock_character(player_id, character_id):
        return jsonify({"message": f"Unlocked {character_id}!", "character_id": character_id}), 200

    return jsonify({"error": "Failed to unlock"}), 500

@bp.route("/<character_id>/equip", methods=["POST"])
def equip_character(character_id: str):
    player_id = get_current_player_id()
    if not player_id:
        return jsonify({"error": "Unauthorized"}), 401

    player_model = get_player_model()
    player_doc = player_model.find_by_id(player_id)
    if not player_doc:
        return jsonify({"error": "Player not found"}), 404

    if character_id not in player_doc.get("unlocked_characters", ["aura"]):
        return jsonify({"error": "Character not unlocked"}), 400

    if player_model.equip_character(player_id, character_id):
        return jsonify({"message": f"Equipped {character_id}", "character_id": character_id}), 200

    return jsonify({"error": "Failed to equip"}), 500

@bp.route("/perk/<character_id>", methods=["GET"])
def get_character_perk(character_id: str):
    """Get perk data for a character (for Unity client to apply)."""
    char_model = get_character_model()
    perk = char_model.get_perk_for_character(character_id)
    if not perk:
        return jsonify({"error": "Character not found or no perk"}), 404

    return jsonify({"perk": perk}), 200
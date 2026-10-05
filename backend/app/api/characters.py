from flask import Blueprint, jsonify

bp = Blueprint("characters", __name__)

@bp.route("", methods=["GET"])
def list_characters():
    # TODO: Return all characters with unlock requirements
    return jsonify({"message": "List characters - TODO"}), 501

@bp.route("/<character_id>/unlock", methods=["POST"])
def unlock_character(character_id):
    # TODO: Auth, check requirements, unlock
    return jsonify({"message": f"Unlock {character_id} - TODO"}), 501
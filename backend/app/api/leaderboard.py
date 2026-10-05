from flask import Blueprint, request, jsonify

bp = Blueprint("leaderboard", __name__)

@bp.route("", methods=["GET"])
def get_leaderboard():
    # TODO: Query top scores with pagination
    return jsonify({"message": "Get leaderboard - TODO"}), 501
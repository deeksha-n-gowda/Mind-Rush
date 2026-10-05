from flask import Blueprint, request, jsonify

bp = Blueprint("scores", __name__)

@bp.route("", methods=["POST"])
def submit_score():
    # TODO: Auth, validate, save score
    return jsonify({"message": "Submit score - TODO"}), 501

@bp.route("/my", methods=["GET"])
def my_scores():
    # TODO: Auth, return user's scores
    return jsonify({"message": "Get my scores - TODO"}), 501
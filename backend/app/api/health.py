from flask import Blueprint, jsonify

bp = Blueprint("health", __name__)

@bp.route("/health", methods=["GET"])
def health_check():
    return jsonify({
        "status": "healthy",
        "service": "mindrush-api",
        "version": "1.0.0"
    }), 200
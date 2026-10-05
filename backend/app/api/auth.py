from flask import Blueprint, request, jsonify, current_app
import jwt
import datetime
import os
from app.models import Player

bp = Blueprint("auth", __name__)

def get_player_model():
    return Player(current_app.mongo)

def generate_token(player_id: str) -> str:
    payload = {
        "sub": player_id,
        "iat": datetime.datetime.utcnow(),
        "exp": datetime.datetime.utcnow() + datetime.timedelta(hours=24)
    }
    return jwt.encode(payload, current_app.config["JWT_SECRET"], algorithm=current_app.config["JWT_ALGORITHM"])

def decode_token(token: str) -> dict | None:
    try:
        return jwt.decode(token, current_app.config["JWT_SECRET"], algorithms=[current_app.config["JWT_ALGORITHM"]])
    except jwt.ExpiredSignatureError:
        return None
    except jwt.InvalidTokenError:
        return None

def get_current_player_id() -> str | None:
    auth_header = request.headers.get("Authorization", "")
    if not auth_header.startswith("Bearer "):
        return None
    token = auth_header[7:]
    payload = decode_token(token)
    if not payload:
        return None
    return payload.get("sub")

@bp.route("/register", methods=["POST"])
def register():
    data = request.get_json() or {}
    username = data.get("username", "").strip()
    email = data.get("email", "").strip().lower()
    password = data.get("password", "")

    if not username or not email or not password:
        return jsonify({"error": "Username, email, and password required"}), 400
    if len(username) < 3 or len(username) > 20:
        return jsonify({"error": "Username must be 3-20 characters"}), 400
    if len(password) < 6:
        return jsonify({"error": "Password must be at least 6 characters"}), 400

    player_model = get_player_model()

    if player_model.find_by_username(username):
        return jsonify({"error": "Username already taken"}), 409
    if player_model.find_by_email(email):
        return jsonify({"error": "Email already registered"}), 409

    player_doc = player_model.create(username, email, password)
    token = generate_token(str(player_doc["_id"]))

    return jsonify({
        "message": "Registered successfully",
        "token": token,
        "player": player_model.to_public_dict(player_doc)
    }), 201

@bp.route("/login", methods=["POST"])
def login():
    data = request.get_json() or {}
    username = data.get("username", "").strip()
    password = data.get("password", "")

    if not username or not password:
        return jsonify({"error": "Username and password required"}), 400

    player_model = get_player_model()
    player_doc = player_model.find_by_username(username)

    if not player_doc or not player_model.verify_password(player_doc, password):
        return jsonify({"error": "Invalid credentials"}), 401

    token = generate_token(str(player_doc["_id"]))
    return jsonify({
        "message": "Logged in successfully",
        "token": token,
        "player": player_model.to_public_dict(player_doc)
    }), 200

@bp.route("/me", methods=["GET"])
def me():
    player_id = get_current_player_id()
    if not player_id:
        return jsonify({"error": "Unauthorized"}), 401

    player_model = get_player_model()
    player_doc = player_model.find_by_id(player_id)
    if not player_doc:
        return jsonify({"error": "Player not found"}), 404

    return jsonify({"player": player_model.to_public_dict(player_doc)}), 200

@bp.route("/refresh", methods=["POST"])
def refresh():
    player_id = get_current_player_id()
    if not player_id:
        return jsonify({"error": "Unauthorized"}), 401

    player_model = get_player_model()
    player_doc = player_model.find_by_id(player_id)
    if not player_doc:
        return jsonify({"error": "Player not found"}), 404

    token = generate_token(player_id)
    return jsonify({"token": token}), 200
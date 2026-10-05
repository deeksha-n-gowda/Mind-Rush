from flask import Blueprint, request, jsonify
import jwt
import datetime
import os

bp = Blueprint("auth", __name__)

@bp.route("/register", methods=["POST"])
def register():
    data = request.get_json()
    # TODO: Validate, hash password, save user
    return jsonify({"message": "Register endpoint - TODO"}), 501

@bp.route("/login", methods=["POST"])
def login():
    data = request.get_json()
    # TODO: Verify credentials, generate JWT
    return jsonify({"message": "Login endpoint - TODO"}), 501

@bp.route("/me", methods=["GET"])
def me():
    # TODO: Validate JWT, return user profile
    return jsonify({"message": "Get current user - TODO"}), 501
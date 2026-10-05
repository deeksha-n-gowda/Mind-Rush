from flask import Flask
from flask_pymongo import PyMongo
import os

mongo = PyMongo()

def create_app():
    app = Flask(__name__)
    
    # Config
    app.config["MONGO_URI"] = os.getenv("MONGO_URI", "mongodb://localhost:27017/mindrush")
    app.config["JWT_SECRET"] = os.getenv("JWT_SECRET", "dev-secret-change-me")
    app.config["JWT_ALGORITHM"] = os.getenv("JWT_ALGORITHM", "HS256")
    app.config["JWT_EXPIRY_HOURS"] = int(os.getenv("JWT_EXPIRY_HOURS", "24"))
    
    # Init extensions
    mongo.init_app(app)
    
    # Register blueprints
    from app.api.health import bp as health_bp
    from app.api.auth import bp as auth_bp
    from app.api.scores import bp as scores_bp
    from app.api.characters import bp as characters_bp
    from app.api.leaderboard import bp as leaderboard_bp
    from app.api.mimi import bp as mimi_bp
    
    app.register_blueprint(health_bp)
    app.register_blueprint(auth_bp, url_prefix="/api/auth")
    app.register_blueprint(scores_bp, url_prefix="/api/scores")
    app.register_blueprint(characters_bp, url_prefix="/api/characters")
    app.register_blueprint(leaderboard_bp, url_prefix="/api/leaderboard")
    app.register_blueprint(mimi_bp, url_prefix="/api/mimi")
    
    return app
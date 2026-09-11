import os
from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    PROJECT_NAME: str = "QLNS HR & ATS Recruitment API"
    VERSION: str = "1.0.0"
    API_V1_STR: str = "/api"
    DATABASE_URL: str = os.getenv(
        "DATABASE_URL", 
        "postgresql://qlns_user:qlns_password@localhost:5432/qlns_db"
    )

    class Config:
        case_sensitive = True

settings = Settings()

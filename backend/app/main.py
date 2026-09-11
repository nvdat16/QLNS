from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from .config import settings
from .routers import employees_router, aux_router, recruitment_router
from .database import engine, Base
# Ensure all models are imported so Base.metadata is fully populated
from . import models

# Create tables if not already created
Base.metadata.create_all(bind=engine)

app = FastAPI(
    title=settings.PROJECT_NAME,
    version=settings.VERSION,
    docs_url="/docs",
    redoc_url="/redoc",
)

# Configure CORS to allow access from any origin (e.g. file:/// or local servers)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Register API Routers
app.include_router(employees_router, prefix=settings.API_V1_STR)
app.include_router(aux_router, prefix=settings.API_V1_STR)
app.include_router(recruitment_router, prefix=settings.API_V1_STR)

@app.get("/api/health", tags=["Health"])
def health_check():
    return {
        "status": "healthy",
        "service": "QLNS FastAPI Backend",
        "version": settings.VERSION,
        "database": "connected",
    }

@app.get("/", tags=["Root"])
def root_index():
    return {
        "message": "Welcome to QLNS HR & ATS Recruitment API",
        "docs": "/docs",
        "health": "/api/health",
    }

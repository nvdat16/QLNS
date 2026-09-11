from datetime import date, datetime
from typing import Optional, List, Any, Dict
from decimal import Decimal
from pydantic import BaseModel, ConfigDict

# Job Posting Schemas
class JobPostingBase(BaseModel):
    title: str
    department_id: int
    job_code: Optional[str] = None
    description: Optional[str] = None
    requirements: Optional[str] = None
    location: Optional[str] = "Hanoi / Hybrid"
    employment_type: Optional[str] = "Full-time"
    salary_min: Optional[Decimal] = None
    salary_max: Optional[Decimal] = None
    target_headcount: Optional[int] = 1
    status: Optional[str] = "Active Recruiting"
    channels: Optional[str] = "LinkedIn, TopCV, Careers"

class JobPostingCreate(JobPostingBase):
    pass

class JobPostingResponse(JobPostingBase):
    id: int
    department_name: Optional[str] = None
    applicant_count: Optional[int] = 0
    published_at: Optional[datetime] = None
    created_at: Optional[datetime] = None
    model_config = ConfigDict(from_attributes=True)


# Candidate Schemas
class CandidateBase(BaseModel):
    first_name: str
    last_name: str
    email: str
    phone: Optional[str] = None
    avatar_url: Optional[str] = None
    address: Optional[str] = None
    linkedin_url: Optional[str] = None
    portfolio_url: Optional[str] = None

class CandidateCreate(CandidateBase):
    pass

class CandidateResponse(CandidateBase):
    id: int
    full_name: Optional[str] = None
    created_at: Optional[datetime] = None
    model_config = ConfigDict(from_attributes=True)


# Application & Pipeline Schemas
class PipelineCard(BaseModel):
    id: int
    application_id: int
    candidate_id: int
    name: str
    role: str
    ai_score: int
    avatar_url: Optional[str] = None
    stage: str
    source: str
    skills: List[str] = []
    metadata: Dict[str, Any] = {}
    model_config = ConfigDict(from_attributes=True)

class PipelineStage(BaseModel):
    stage_id: str
    stage_name: str
    color: str
    count: int
    cards: List[PipelineCard] = []

class PipelineResponse(BaseModel):
    stages: List[PipelineStage]
    total_candidates: int
    total_active_jobs: int

class AdvanceStageRequest(BaseModel):
    next_stage: str
    note: Optional[str] = None

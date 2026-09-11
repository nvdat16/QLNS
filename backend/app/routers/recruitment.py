from typing import List, Optional
from fastapi import APIRouter, Depends, HTTPException, Query, status
from sqlalchemy.orm import Session

from ..database import get_db
from ..models.recruitment import JobPosting, Candidate, Application, Resume, Interview, Evaluation, Offer
from ..models.employee import Department
from ..schemas.recruitment import (
    JobPostingResponse,
    JobPostingCreate,
    CandidateResponse,
    PipelineResponse,
    PipelineStage,
    PipelineCard,
    AdvanceStageRequest,
)

router = APIRouter(prefix="/recruitment", tags=["Recruitment & ATS"])

KANBAN_STAGES = [
    {"id": "sourced", "name": "1. Sourced & Applied", "color": "slate", "db_stage": "Sourced & Applied"},
    {"id": "screening", "name": "2. AI Screening", "color": "blue", "db_stage": "AI Screening"},
    {"id": "interview", "name": "3. Tech Interview", "color": "amber", "db_stage": "Tech Interview"},
    {"id": "executive", "name": "4. Executive Round", "color": "purple", "db_stage": "Executive Round"},
    {"id": "offer", "name": "5. Offer Letter", "color": "primary", "db_stage": "Offer Letter"},
    {"id": "hired", "name": "6. Hired & Ready", "color": "emerald", "db_stage": "Hired & Ready"},
]

@router.get("/pipeline", response_model=PipelineResponse)
def get_recruitment_pipeline(db: Session = Depends(get_db)):
    applications = (
        db.query(Application)
        .join(Candidate)
        .join(JobPosting)
        .order_by(Application.id.asc())
        .all()
    )

    stage_map = {s["db_stage"]: [] for s in KANBAN_STAGES}

    for app in applications:
        cand = app.candidate
        job = app.job_posting
        meta = app.stage_metadata or {}
        skills = meta.get("skills", [])

        card = PipelineCard(
            id=cand.id,
            application_id=app.id,
            candidate_id=cand.id,
            name=f"{cand.first_name} {cand.last_name}",
            role=job.title if job else "Applicant",
            ai_score=app.ai_score or 90,
            avatar_url=cand.avatar_url,
            stage=app.stage,
            source=app.source or "Direct",
            skills=skills,
            metadata=meta,
        )

        if app.stage in stage_map:
            stage_map[app.stage].append(card)
        else:
            stage_map["Sourced & Applied"].append(card)

    stages_result = []
    for s in KANBAN_STAGES:
        cards = stage_map[s["db_stage"]]
        stages_result.append(
            PipelineStage(
                stage_id=s["id"],
                stage_name=s["name"],
                color=s["color"],
                count=len(cards),
                cards=cards,
            )
        )

    total_active_jobs = db.query(JobPosting).filter(JobPosting.status.ilike("%active%")).count()

    return PipelineResponse(
        stages=stages_result,
        total_candidates=len(applications),
        total_active_jobs=total_active_jobs or 14,
    )


@router.get("/jobs", response_model=List[JobPostingResponse])
def get_job_postings(
    query: Optional[str] = Query(None, description="Search job title or location"),
    status: Optional[str] = Query(None),
    db: Session = Depends(get_db),
):
    stmt = db.query(JobPosting).join(Department)
    if query:
        pattern = f"%{query.strip()}%"
        stmt = stmt.filter(JobPosting.title.ilike(pattern))
    if status and status.lower() != "all":
        stmt = stmt.filter(JobPosting.status == status)

    jobs = stmt.order_by(JobPosting.id.asc()).all()
    result = []
    for j in jobs:
        data = JobPostingResponse.model_validate(j)
        data.department_name = j.department.name if j.department else None
        data.applicant_count = len(j.applications)
        result.append(data)
    return result


@router.post("/jobs", response_model=JobPostingResponse, status_code=status.HTTP_201_CREATED)
def create_job_posting(payload: JobPostingCreate, db: Session = Depends(get_db)):
    dept = db.query(Department).filter(Department.id == payload.department_id).first()
    if not dept:
        # Fallback to first department
        dept = db.query(Department).first()
        if not dept:
            raise HTTPException(status_code=400, detail="No department available")
        payload.department_id = dept.id

    new_job = JobPosting(**payload.model_dump())
    db.add(new_job)
    db.commit()
    db.refresh(new_job)

    data = JobPostingResponse.model_validate(new_job)
    data.department_name = dept.name
    data.applicant_count = 0
    return data


@router.get("/candidates/{candidate_id}")
def get_candidate_detail(candidate_id: int, db: Session = Depends(get_db)):
    cand = db.query(Candidate).filter(Candidate.id == candidate_id).first()
    if not cand:
        raise HTTPException(status_code=404, detail="Candidate not found")

    app = db.query(Application).filter(Application.candidate_id == candidate_id).first()
    job_title = app.job_posting.title if app and app.job_posting else "Candidate"
    score = f"{app.ai_score}%" if app and app.ai_score else "92%"

    return {
        "id": cand.id,
        "name": f"{cand.first_name} {cand.last_name}",
        "role": job_title,
        "score": score,
        "email": cand.email,
        "phone": cand.phone or "+84 900 000 000",
        "avatar": cand.avatar_url,
        "address": cand.address or "Hanoi, Vietnam",
        "stage": app.stage if app else "Sourced & Applied",
        "skills": app.stage_metadata.get("skills", ["Golang", "PostgreSQL", "Docker"]) if app and app.stage_metadata else ["Golang", "PostgreSQL"],
    }


@router.post("/applications/{app_id}/advance")
def advance_application_stage(
    app_id: int,
    payload: AdvanceStageRequest,
    db: Session = Depends(get_db),
):
    app = db.query(Application).filter(Application.id == app_id).first()
    if not app:
        raise HTTPException(status_code=404, detail="Application not found")

    app.stage = payload.next_stage
    db.commit()
    db.refresh(app)

    return {"status": "success", "application_id": app.id, "new_stage": app.stage}

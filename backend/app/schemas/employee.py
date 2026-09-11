from datetime import date, datetime
from typing import Optional, List
from decimal import Decimal
from pydantic import BaseModel, ConfigDict

# Department Schemas
class DepartmentBase(BaseModel):
    name: str
    code: str
    description: Optional[str] = None

class DepartmentResponse(DepartmentBase):
    id: int
    created_at: Optional[datetime] = None
    model_config = ConfigDict(from_attributes=True)


# Position Schemas
class PositionBase(BaseModel):
    name: str
    code: str
    level: Optional[str] = None
    description: Optional[str] = None

class PositionResponse(PositionBase):
    id: int
    created_at: Optional[datetime] = None
    model_config = ConfigDict(from_attributes=True)


# Contract Schemas
class ContractBase(BaseModel):
    protocol_number: Optional[str] = None
    contract_type: str
    start_date: date
    end_date: Optional[date] = None
    notice_period: Optional[str] = None
    salary: Optional[Decimal] = None
    status: Optional[str] = "Active"
    document_url: Optional[str] = None

class ContractCreate(ContractBase):
    employee_id: int

class ContractResponse(ContractBase):
    id: int
    employee_id: int
    created_at: Optional[datetime] = None
    model_config = ConfigDict(from_attributes=True)


# Employee Schemas
class EmployeeBase(BaseModel):
    employee_code: str
    first_name: str
    last_name: str
    email: str
    phone: Optional[str] = None
    avatar_url: Optional[str] = None
    office_location: Optional[str] = None
    work_auth: Optional[str] = None
    emergency_contact: Optional[str] = None
    cost_center: Optional[str] = None
    manager_id: Optional[int] = None
    hire_date: Optional[date] = None
    status: Optional[str] = "Active"
    department_id: int
    position_id: int

class EmployeeCreate(EmployeeBase):
    pass

class EmployeeUpdate(BaseModel):
    first_name: Optional[str] = None
    last_name: Optional[str] = None
    email: Optional[str] = None
    phone: Optional[str] = None
    office_location: Optional[str] = None
    work_auth: Optional[str] = None
    status: Optional[str] = None
    department_id: Optional[int] = None
    position_id: Optional[int] = None

class EmployeeResponse(EmployeeBase):
    id: int
    full_name: Optional[str] = None
    department_name: Optional[str] = None
    position_name: Optional[str] = None
    position_level: Optional[str] = None
    created_at: Optional[datetime] = None
    updated_at: Optional[datetime] = None
    model_config = ConfigDict(from_attributes=True)

class EmployeeDetailResponse(EmployeeResponse):
    department: Optional[DepartmentResponse] = None
    position: Optional[PositionResponse] = None
    contracts: List[ContractResponse] = []

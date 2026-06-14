// Shared DTO shapes mirroring the backend contracts (api/v1).

// The API serializes enums as numbers: Employee=0, Manager=1, HrAdmin=2, Finance=3.
export type UserRole = number;
export const RoleLabels = ['Employee', 'Manager', 'HrAdmin', 'Finance'];

export interface UserDto {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
}

export interface AuthResult {
  token: string;
  expiresAtUtc: string;
  user: UserDto;
}

export interface BenefitPlanDto {
  id: string;
  name: string;
  description: string;
  category: number;
  monthlyCost: number;
  isActive: boolean;
}

export interface BenefitEnrollmentDto {
  id: string;
  employeeId: string;
  benefitPlanId: string;
  benefitPlanName: string;
  coverageStartDate: string;
  status: number;
}

export interface ClaimDto {
  id: string;
  employeeId: string;
  type: number;
  amount: number;
  description: string;
  status: number;
  hasReceipt: boolean;
  receiptFileName?: string;
  ocrText?: string;
  ocrExtractedAmount?: number;
  reviewerId?: string;
  decisionNotes?: string;
  submittedAtUtc: string;
  settledAtUtc?: string;
  payrollReference?: string;
  history: ClaimTransitionDto[];
}

export interface ClaimTransitionDto {
  fromStatus?: number;
  toStatus: number;
  actorId: string;
  notes?: string;
  occurredAtUtc: string;
}

export interface CompensationStructureDto {
  id: string;
  employeeId: string;
  grade: number;
  annualBasic: number;
  effectiveFrom: string;
  status: number;
  grossAnnual: number;
  totalDeductions: number;
  netAnnual: number;
  components: { name: string; amount: number; type: number }[];
}

export interface PayslipDto {
  id: string;
  employeeId: string;
  year: number;
  month: number;
  grossMonthly: number;
  totalDeductionsMonthly: number;
  netMonthly: number;
  generatedAtUtc: string;
}

export interface SettlementRequestDto {
  id: string;
  employeeId: string;
  amount: number;
  reference: string;
  status: number;
  attempts: number;
  payrollConfirmation?: string;
  lastError?: string;
  requestedAtUtc: string;
  completedAtUtc?: string;
}

export interface PromotionNominationDto {
  id: string;
  employeeId: string;
  nominatedById: string;
  currentGrade: number;
  proposedGrade: number;
  justification: string;
  status: number;
  effectiveDate?: string;
  decisionNotes?: string;
  nominatedAtUtc: string;
}

// Enum label maps for display.
export const ClaimStatusLabels = ['Submitted', 'UnderReview', 'Approved', 'Rejected', 'Settled'];
export const ClaimTypeLabels = ['Travel', 'Medical', 'Food', 'Internet', 'Training', 'Other'];
export const BenefitCategoryLabels = ['Health', 'Wellness', 'Insurance', 'Transport', 'Food', 'Education'];
export const EnrollmentStatusLabels = ['Active', 'Cancelled'];
export const CompensationStatusLabels = ['Draft', 'Approved'];
export const GradeBandLabels = ['Junior', 'Mid', 'Senior', 'Lead'];
export const PromotionStatusLabels = ['Nominated', 'UnderReview', 'Approved', 'Rejected'];
export const SettlementStatusLabels = ['Pending', 'Processing', 'Succeeded', 'Failed'];

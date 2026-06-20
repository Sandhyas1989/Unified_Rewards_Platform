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
  currencyCode?: string;
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
  currencyCode?: string;
  description: string;
  status: number;
  hasReceipt: boolean;
  receiptFileName?: string;
  ocrText?: string;
  ocrExtractedAmount?: number;
  reviewerId?: string;
  decisionNotes?: string;
  settlementReference?: string;
  submittedAtUtc: string;
  decisionAtUtc?: string;
  settledAtUtc?: string;
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
  currencyCode?: string;
  reference: string;
  status: number;
  attempts: number;
  payrollConfirmation?: string;
  lastError?: string;
  requestedAtUtc: string;
  completedAtUtc?: string;
}

export interface PromotionDto {
  id: string;
  title: string;
  cycleYear: number;
  cycleQuarter: string;
  fromGrade: string;
  bonusValue: number;
  nominationStart: string;
  nominationEnd: string;
  status: number;
  nominationCount: number;
  approvedCount: number;
  createdAtUtc: string;
}

export interface NominationDto {
  id: string;
  promotionId: string;
  employeeId: string;
  employeeName?: string;
  nominatedBy: string;
  nominatedOn: string;
  outcome: number;
  remarks?: string;
  createdAtUtc: string;
}

// Enum label maps for display.
export const ClaimStatusLabels = ['Submitted', 'UnderReview', 'Approved', 'Rejected', 'Settled'];
export const ClaimTypeLabels = ['Travel', 'Medical', 'Food', 'Internet', 'Training', 'Other'];
export const BenefitCategoryLabels = ['Health', 'Wellness', 'Insurance', 'Transport', 'Food', 'Education'];
export const EnrollmentStatusLabels = ['Active', 'Cancelled'];
export const CompensationStatusLabels = ['Draft', 'Approved'];
export const GradeBandLabels = ['Junior', 'Mid', 'Senior', 'Lead'];
export const PromotionStatusLabels = ['Draft', 'Open', 'Closed', 'Cancelled'];
export const NominationOutcomeLabels = ['Pending', 'Awarded', 'Not Awarded', 'Withdrawn'];
export const SettlementStatusLabels = ['Pending', 'Processing', 'Succeeded', 'Failed'];

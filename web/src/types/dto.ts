export interface BaseFields {
  id: number;
  userId: number;
  status: number;
  createdAt: string;
}

// Finance
export interface Goal extends BaseFields { name: string; description?: string; targetAmount: number; currentAmount: number; currencyCode: string; dueDate: string; }
export interface Bill extends BaseFields { name: string; description?: string; category: string; amount: number; dueDay: number; paymentMethod?: string; currencyCode: string; isRecurrent: boolean; endDate?: string; recurrenceType?: string; dueDate: string; paidThisMonth: boolean; paidDate?: string; }
export interface Budget extends BaseFields { name: string; description?: string; amountLimit: number; currencyCode: string; startDate: string; endDate: string; }
export interface Earning extends BaseFields { category: string; paymentMethod: string; currencyCode: string; amount: number; description?: string; earningDate: string; }
export interface Expense extends BaseFields { category: string; paymentMethod: string; currencyCode: string; amount: number; description?: string; expenseDate: string; }
export interface Investment extends BaseFields { investmentType: string; category: string; assetName: string; broker?: string; currencyCode: string; investedAmount: number; currentValue?: number; purchaseDate: string; maturityDate?: string; annualYieldPercent?: number; profitLoss?: number; }

// Body
export interface WeeklyRoutine extends BaseFields { dayOfWeek: number; routineName: string; description?: string; }
export interface Workout extends BaseFields { workoutDate: string; routineName: string; durationMinutes?: number; caloriesBurned?: number; notes?: string; }
export interface PersonalRecord extends BaseFields { exerciseName: string; metricType: string; value: number; unit: string; achievedDate: string; notes?: string; }
export interface Meal extends BaseFields { mealDate: string; mealType: string; description?: string; calories: number; proteinGrams?: number; carbsGrams?: number; fatGrams?: number; }
export interface WaterIntake extends BaseFields { intakeDate: string; amountMl: number; }
export interface BodyMetric extends BaseFields { measuredDate: string; weightKg?: number; heightCm?: number; bodyFatPercent?: number; notes?: string; }
export interface SleepLog extends BaseFields { bedTime: string; wakeTime: string; totalHours: number; notes?: string; }

// Wellbeing (mind schema)
export interface MeditationSession extends BaseFields { sessionDate: string; durationMinutes: number; meditationType: string; moodBefore?: number; moodAfter?: number; notes?: string; }
export interface JournalEntry extends BaseFields { entryDate: string; title?: string; content: string; mood?: number; category?: string; }

// Auth
export interface AuthUser { id: number; email: string; username: string; plan: number; }
export interface RegisterRequest { username: string; phoneNumber: string; email: string; password: string; }
export interface LoginRequest { email: string; password: string; }

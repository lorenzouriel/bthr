export type FieldType = 'text' | 'textarea' | 'number' | 'date' | 'datetime' | 'checkbox';

export interface FieldConfig {
  name: string;
  label: string;
  type: FieldType;
  required: boolean;
  readOnly?: boolean;
  maxLength?: number;
}

export interface ResourceConfig {
  key: string;
  section: 'finance' | 'body' | 'wellbeing';
  label: string;
  basePath: string;
  hasEdit: boolean;
  hasDelete: boolean;
  listPrimary: string;
  listSecondary: string[];
  listValue?: string;
  dateField?: string;
  fields: FieldConfig[];
}

export const RESOURCES: ResourceConfig[] = [
  { key: 'goals', section: 'finance', label: 'Goals', basePath: '/api/users/{userId}/goals', hasEdit: true, hasDelete: true,
    listPrimary: 'name', listSecondary: ['currencyCode', 'dueDate'], listValue: 'currentAmount', dateField: 'dueDate',
    fields: [
      { name: 'name', label: 'Name', type: 'text', required: true, maxLength: 100 },
      { name: 'description', label: 'Description', type: 'textarea', required: false, maxLength: 255 },
      { name: 'targetAmount', label: 'Target amount', type: 'number', required: true },
      { name: 'currentAmount', label: 'Current amount', type: 'number', required: true },
      { name: 'currencyCode', label: 'Currency', type: 'text', required: true, maxLength: 10 },
      { name: 'dueDate', label: 'Due date', type: 'date', required: true } ] },

  { key: 'bills', section: 'finance', label: 'Bills', basePath: '/api/users/{userId}/bills', hasEdit: true, hasDelete: true,
    listPrimary: 'name', listSecondary: ['category', 'dueDay'], listValue: 'amount',
    fields: [
      { name: 'name', label: 'Name', type: 'text', required: true, maxLength: 255 },
      { name: 'category', label: 'Category', type: 'text', required: true, maxLength: 100 },
      { name: 'paymentMethod', label: 'Payment method', type: 'text', required: false, maxLength: 100 },
      { name: 'amount', label: 'Amount', type: 'number', required: true },
      { name: 'currencyCode', label: 'Currency', type: 'text', required: true, maxLength: 10 },
      { name: 'dueDay', label: 'Due day (1-31)', type: 'number', required: true },
      { name: 'isRecurrent', label: 'Recurrent', type: 'checkbox', required: false },
      { name: 'endDate', label: 'End date', type: 'date', required: false },
      { name: 'description', label: 'Description', type: 'textarea', required: false, maxLength: 500 },
      { name: 'dueDate', label: 'Computed due date', type: 'text', required: false, readOnly: true },
      { name: 'paidThisMonth', label: 'Paid this month', type: 'checkbox', required: false, readOnly: true } ] },

  { key: 'budgets', section: 'finance', label: 'Budgets', basePath: '/api/users/{userId}/budgets', hasEdit: true, hasDelete: true,
    listPrimary: 'name', listSecondary: ['startDate', 'endDate'], listValue: 'amountLimit', dateField: 'startDate',
    fields: [
      { name: 'name', label: 'Name', type: 'text', required: true, maxLength: 100 },
      { name: 'description', label: 'Description', type: 'textarea', required: false, maxLength: 255 },
      { name: 'amountLimit', label: 'Amount limit', type: 'number', required: true },
      { name: 'currencyCode', label: 'Currency', type: 'text', required: true, maxLength: 10 },
      { name: 'startDate', label: 'Start date', type: 'date', required: true },
      { name: 'endDate', label: 'End date', type: 'date', required: true } ] },

  { key: 'earnings', section: 'finance', label: 'Earnings', basePath: '/api/users/{userId}/earnings', hasEdit: true, hasDelete: true,
    listPrimary: 'category', listSecondary: ['paymentMethod', 'earningDate'], listValue: 'amount', dateField: 'earningDate',
    fields: [
      { name: 'category', label: 'Category', type: 'text', required: true, maxLength: 255 },
      { name: 'paymentMethod', label: 'Payment method', type: 'text', required: true, maxLength: 255 },
      { name: 'currencyCode', label: 'Currency', type: 'text', required: true, maxLength: 10 },
      { name: 'amount', label: 'Amount', type: 'number', required: true },
      { name: 'description', label: 'Description', type: 'textarea', required: false, maxLength: 255 },
      { name: 'earningDate', label: 'Earning date', type: 'date', required: true } ] },

  { key: 'expenses', section: 'finance', label: 'Expenses', basePath: '/api/users/{userId}/expenses', hasEdit: true, hasDelete: true,
    listPrimary: 'category', listSecondary: ['paymentMethod', 'expenseDate'], listValue: 'amount', dateField: 'expenseDate',
    fields: [
      { name: 'category', label: 'Category', type: 'text', required: true, maxLength: 255 },
      { name: 'paymentMethod', label: 'Payment method', type: 'text', required: true, maxLength: 255 },
      { name: 'currencyCode', label: 'Currency', type: 'text', required: true, maxLength: 10 },
      { name: 'amount', label: 'Amount', type: 'number', required: true },
      { name: 'description', label: 'Description', type: 'textarea', required: false, maxLength: 255 },
      { name: 'expenseDate', label: 'Expense date', type: 'date', required: true } ] },

  { key: 'investments', section: 'finance', label: 'Investments', basePath: '/api/users/{userId}/investments', hasEdit: true, hasDelete: true,
    listPrimary: 'assetName', listSecondary: ['investmentType', 'purchaseDate'], listValue: 'investedAmount', dateField: 'purchaseDate',
    fields: [
      { name: 'investmentType', label: 'Type', type: 'text', required: true, maxLength: 50 },
      { name: 'category', label: 'Category', type: 'text', required: true, maxLength: 100 },
      { name: 'assetName', label: 'Asset name', type: 'text', required: true, maxLength: 100 },
      { name: 'broker', label: 'Broker', type: 'text', required: false, maxLength: 100 },
      { name: 'currencyCode', label: 'Currency', type: 'text', required: true, maxLength: 10 },
      { name: 'investedAmount', label: 'Invested amount', type: 'number', required: true },
      { name: 'currentValue', label: 'Current value', type: 'number', required: false },
      { name: 'purchaseDate', label: 'Purchase date', type: 'date', required: true },
      { name: 'maturityDate', label: 'Maturity date', type: 'date', required: false },
      { name: 'annualYieldPercent', label: 'Annual yield %', type: 'number', required: false },
      { name: 'profitLoss', label: 'Profit/loss', type: 'number', required: false } ] },

  { key: 'weekly-routines', section: 'body', label: 'Weekly Routines', basePath: '/api/users/{userId}/body/weekly-routines', hasEdit: true, hasDelete: true,
    listPrimary: 'routineName', listSecondary: ['dayOfWeek'],
    fields: [
      { name: 'dayOfWeek', label: 'Day of week (0-6)', type: 'number', required: true },
      { name: 'routineName', label: 'Routine name', type: 'text', required: true, maxLength: 100 },
      { name: 'description', label: 'Description', type: 'textarea', required: false, maxLength: 500 } ] },

  { key: 'workouts', section: 'body', label: 'Workouts', basePath: '/api/users/{userId}/body/workouts', hasEdit: true, hasDelete: true,
    listPrimary: 'routineName', listSecondary: ['workoutDate'], listValue: 'durationMinutes', dateField: 'workoutDate',
    fields: [
      { name: 'workoutDate', label: 'Workout date', type: 'date', required: true },
      { name: 'routineName', label: 'Routine name', type: 'text', required: true, maxLength: 100 },
      { name: 'durationMinutes', label: 'Duration (min)', type: 'number', required: false },
      { name: 'caloriesBurned', label: 'Calories burned', type: 'number', required: false },
      { name: 'notes', label: 'Notes', type: 'textarea', required: false, maxLength: 500 } ] },

  { key: 'personal-records', section: 'body', label: 'Personal Records', basePath: '/api/users/{userId}/body/personal-records', hasEdit: false, hasDelete: false,
    listPrimary: 'exerciseName', listSecondary: ['metricType', 'unit'], listValue: 'value', dateField: 'achievedDate',
    fields: [
      { name: 'exerciseName', label: 'Exercise', type: 'text', required: true, maxLength: 100 },
      { name: 'metricType', label: 'Metric type', type: 'text', required: true, maxLength: 50 },
      { name: 'value', label: 'Value', type: 'number', required: true },
      { name: 'unit', label: 'Unit', type: 'text', required: true, maxLength: 20 },
      { name: 'achievedDate', label: 'Achieved date', type: 'date', required: true },
      { name: 'notes', label: 'Notes', type: 'textarea', required: false, maxLength: 500 } ] },

  { key: 'meals', section: 'body', label: 'Meals', basePath: '/api/users/{userId}/body/meals', hasEdit: true, hasDelete: true,
    listPrimary: 'mealType', listSecondary: ['mealDate'], listValue: 'calories', dateField: 'mealDate',
    fields: [
      { name: 'mealDate', label: 'Meal date', type: 'date', required: true },
      { name: 'mealType', label: 'Meal type', type: 'text', required: true, maxLength: 50 },
      { name: 'description', label: 'Description', type: 'textarea', required: false, maxLength: 500 },
      { name: 'calories', label: 'Calories', type: 'number', required: true },
      { name: 'proteinGrams', label: 'Protein (g)', type: 'number', required: false },
      { name: 'carbsGrams', label: 'Carbs (g)', type: 'number', required: false },
      { name: 'fatGrams', label: 'Fat (g)', type: 'number', required: false } ] },

  { key: 'water-intake', section: 'body', label: 'Water Intake', basePath: '/api/users/{userId}/body/water-intake', hasEdit: true, hasDelete: true,
    listPrimary: 'intakeDate', listSecondary: [], listValue: 'amountMl', dateField: 'intakeDate',
    fields: [
      { name: 'intakeDate', label: 'Date', type: 'date', required: true },
      { name: 'amountMl', label: 'Amount (ml)', type: 'number', required: true } ] },

  { key: 'body-metrics', section: 'body', label: 'Body Metrics', basePath: '/api/users/{userId}/body/body-metrics', hasEdit: true, hasDelete: true,
    listPrimary: 'measuredDate', listSecondary: ['bodyFatPercent'], listValue: 'weightKg', dateField: 'measuredDate',
    fields: [
      { name: 'measuredDate', label: 'Measured date', type: 'date', required: true },
      { name: 'weightKg', label: 'Weight (kg)', type: 'number', required: false },
      { name: 'heightCm', label: 'Height (cm)', type: 'number', required: false },
      { name: 'bodyFatPercent', label: 'Body fat %', type: 'number', required: false },
      { name: 'notes', label: 'Notes', type: 'textarea', required: false, maxLength: 500 } ] },

  { key: 'sleep-logs', section: 'body', label: 'Sleep Logs', basePath: '/api/users/{userId}/body/sleep-logs', hasEdit: true, hasDelete: true,
    listPrimary: 'bedTime', listSecondary: ['wakeTime'], listValue: 'totalHours', dateField: 'bedTime',
    fields: [
      { name: 'bedTime', label: 'Bed time', type: 'datetime', required: true },
      { name: 'wakeTime', label: 'Wake time', type: 'datetime', required: true },
      { name: 'notes', label: 'Notes', type: 'textarea', required: false, maxLength: 500 },
      { name: 'totalHours', label: 'Total hours', type: 'number', required: false, readOnly: true } ] },

  { key: 'meditation-sessions', section: 'wellbeing', label: 'Meditation', basePath: '/api/users/{userId}/mind/meditation-sessions', hasEdit: true, hasDelete: true,
    listPrimary: 'meditationType', listSecondary: ['sessionDate'], listValue: 'durationMinutes', dateField: 'sessionDate',
    fields: [
      { name: 'sessionDate', label: 'Session date', type: 'date', required: true },
      { name: 'durationMinutes', label: 'Duration (min)', type: 'number', required: true },
      { name: 'meditationType', label: 'Type', type: 'text', required: true, maxLength: 50 },
      { name: 'moodBefore', label: 'Mood before (1-5)', type: 'number', required: false },
      { name: 'moodAfter', label: 'Mood after (1-5)', type: 'number', required: false },
      { name: 'notes', label: 'Notes', type: 'textarea', required: false, maxLength: 500 } ] },

  { key: 'journal-entries', section: 'wellbeing', label: 'Journal', basePath: '/api/users/{userId}/mind/journal-entries', hasEdit: true, hasDelete: true,
    listPrimary: 'title', listSecondary: ['entryDate', 'category'], listValue: 'mood', dateField: 'entryDate',
    fields: [
      { name: 'entryDate', label: 'Entry date', type: 'date', required: true },
      { name: 'title', label: 'Title', type: 'text', required: false, maxLength: 200 },
      { name: 'content', label: 'Content', type: 'textarea', required: true },
      { name: 'mood', label: 'Mood (1-5)', type: 'number', required: false },
      { name: 'category', label: 'Category', type: 'text', required: false, maxLength: 50 } ] },
];

export const SECTIONS = [
  { key: 'finance', label: 'Finance' },
  { key: 'body', label: 'Body' },
  { key: 'wellbeing', label: 'Wellbeing' },
] as const;

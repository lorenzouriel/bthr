"""Populate the specified local test account through the UI API proxy. Credentials come from environment variables."""
import json, random, urllib.request, urllib.error, http.cookiejar, os
from datetime import datetime, timedelta, timezone
from pathlib import Path
random.seed()
base='http://127.0.0.1:5174'
jar=http.cookiejar.CookieJar()
client=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))
def call(path, data=None, method=None):
    req=urllib.request.Request(base+path, data=json.dumps(data).encode() if data is not None else None, headers={'Content-Type':'application/json'}, method=method)
    try:
        with client.open(req,timeout=25) as response:
            text=response.read(); return json.loads(text) if text else None
    except urllib.error.HTTPError as e:
        raise RuntimeError(f'{method or "GET"} {path}: {e.code} {e.read().decode()[:450]}') from None
call('/api/auth/login',dict(email=os.environ['BTHR_TEST_EMAIL'],password=os.environ['BTHR_TEST_PASSWORD']),'POST')
me=call('/api/auth/me')
assert me['email']==os.environ['BTHR_TEST_EMAIL'] and me['plan']>=1
root=f'/api/users/{me["id"]}/'
now=datetime.now(timezone.utc)
def day(offset=0):return (now-timedelta(days=offset)).strftime('%Y-%m-%d')
def amount(a,b):return round(random.uniform(a,b),2)
def pick(items):return random.choice(items)
notes='Random synthetic data for UI testing.'
def save(resource,rows):
    existing=call(root+resource)
    if existing:
        print(resource,'already populated:',len(existing),flush=True); return existing
    for row in rows:call(root+resource,row,'POST')
    result=call(root+resource)
    assert len(result)==len(rows),(resource,len(result),len(rows))
    print(resource,len(result),'created and verified',flush=True)
    return result
save('goals',[dict(name=n,description=notes,targetAmount=10000+i*5000,currentAmount=amount(1000,9000),currencyCode='BRL',dueDate=day(-30*(i+1))) for i,n in enumerate(['Emergency fund','Holiday','New laptop','Home renovation','Education','New bicycle','Car fund','House deposit','Studio equipment','Long-term savings'])])
save('bills',[dict(name=n,category=pick(['Home','Services','Health']),amount=amount(35,1200),currencyCode='BRL',dueDay=i+1,isRecurrent=True,recurrenceType='Monthly',paymentMethod='Bank transfer',description=notes) for i,n in enumerate(['Rent','Electricity','Water','Internet','Mobile','Gym','Streaming','Insurance','Cloud storage','Music','Language classes','Transport pass'])])
save('budgets',[dict(name=f'{n} budget',description=notes,amountLimit=amount(500,3500),currencyCode='BRL',startDate=day(30),endDate=day(-30)) for n in ['Food','Transport','Housing','Health','Travel','Learning','Shopping','Leisure','Gifts','Subscriptions']])
save('earnings',[dict(category=pick(['Salary','Freelance','Refund','Bonus','Side project']),paymentMethod='Bank transfer',currencyCode=random.choices(['BRL','USD','EUR'],weights=[8,1,1])[0],amount=amount(300,6000),description=notes,earningDate=day(i*2)) for i in range(45)])
save('expenses',[dict(category=pick(['Groceries','Dining','Transport','Shopping','Health','Entertainment','Utilities']),paymentMethod=pick(['Credit card','Debit card','PIX','Cash']),currencyCode=random.choices(['BRL','USD','EUR'],weights=[8,1,1])[0],amount=amount(8,600),description=f'{notes} Purchase {i+1}',expenseDate=day(i%90)) for i in range(360)])
save('investments',[dict(investmentType=pick(['Stock','ETF','Bond']),category='Demo portfolio',assetName=f'Demo asset {i+1:02}',broker='Demo broker',currencyCode='BRL',investedAmount=1000+i*100,currentValue=amount(850+i*100,1250+i*100),purchaseDate=day(i*4),annualYieldPercent=amount(1,12)) for i in range(20)])
save('body/weekly-routines',[dict(dayOfWeek=i,routineName=n,description=notes) for i,n in enumerate(['Rest and stretch','Strength A','Easy run','Yoga','Strength B','Swimming','Long walk'])])
save('body/workouts',[dict(workoutDate=day(i),routineName=pick(['Strength A','Strength B','Easy run','Swimming','Long walk','Yoga']),durationMinutes=random.randint(20,90),caloriesBurned=random.randint(120,650),notes=notes) for i in range(100)])
save('body/personal-records',[dict(exerciseName=pick(['Squat','Deadlift','Bench press','5 km run','Plank']),metricType='Weight' if i%2 else 'Time',value=amount(20,120),unit='kg' if i%2 else 'sec',achievedDate=day(i*3),notes=notes) for i in range(25)])
save('body/meals',[dict(mealDate=day(i//3),mealType=['Breakfast','Lunch','Dinner'][i%3],description=pick(['Oats and fruit','Rice, beans and vegetables','Grilled fish and salad','Pasta with vegetables','Chicken and potatoes']),calories=random.randint(250,850),proteinGrams=amount(10,45),carbsGrams=amount(25,100),fatGrams=amount(8,30)) for i in range(360)])
save('body/water-intake',[dict(intakeDate=day(i),amountMl=random.randrange(1000,3200,100)) for i in range(90)])
save('body/body-metrics',[dict(measuredDate=day(i*2),weightKg=amount(72,77),heightCm=175,bodyFatPercent=amount(16,22),notes=notes) for i in range(45)])
save('body/sleep-logs',[dict(bedTime=day(i+1)+'T23:00:00Z',wakeTime=day(i)+f'T{random.randint(5,8):02}:30:00Z',notes=notes) for i in range(100)])
habits=save('body/habits',[dict(habitName=n,category=pick(['Health','Learning','Routine']),targetFrequency='Daily',description=notes) for n in ['Read 20 minutes','Morning walk','Stretch','Practice language','Prepare lunch','No late coffee','Evening reflection','Take screen breaks']])
save('body/habit-logs',[dict(habitId=h['id'],logDate=day(i),isCompleted=random.random()>.22,notes=notes) for h in habits for i in range(60)])
save('body/substance-logs',[dict(consumedAt=day(i)+f'T{random.randint(8,16):02}:00:00Z',substanceType='Caffeine',amount=random.choice([40,80,120]),unit='mg',notes=notes) for i in range(45)])
save('body/symptom-logs',[dict(logDate=day(i*2),symptom=pick(['Headache','Fatigue','Muscle soreness','Congestion']),severity=random.randint(1,5),notes=notes) for i in range(60)])
save('mind/meditation-sessions',[dict(sessionDate=day(i),durationMinutes=random.randint(5,30),meditationType=pick(['Mindfulness','Breathing','Body scan','Guided']),moodBefore=random.randint(1,4),moodAfter=random.randint(3,5),notes=notes) for i in range(100)])
save('mind/journal-entries',[dict(entryDate=day(i),title=f'{pick(["A productive day","Small wins","Time to recharge","New ideas","Grateful moments"])} - {day(i)}',content=f'{notes} Today I made time for a walk, reflected on my goals, and planned a small improvement for tomorrow.',mood=random.randint(1,5),category=pick(['Reflection','Gratitude','Work','Personal'])) for i in range(100)])
summary={}
for resource in ['goals','bills','budgets','earnings','expenses','investments','body/weekly-routines','body/workouts','body/personal-records','body/meals','body/water-intake','body/body-metrics','body/sleep-logs','body/habits','body/habit-logs','body/substance-logs','body/symptom-logs','mind/meditation-sessions','mind/journal-entries']:
    summary[resource]=len(call(root+resource))
for domain in ['finance','body','mind','all']:
    report=call(f'/api/reports/review?start_date={day(29)}&end_date={day()}&domain={domain}&combine=true')
    assert report['metrics'],domain
    print('Report',domain,'verified',len(report['metrics']),'metrics',flush=True)
assert me['plan']>=1
print(json.dumps({'userId':me['id'],'counts':summary,'total':sum(summary.values())},indent=2))
Path('scripts/random-data-summary.json').write_text(json.dumps({'userId':me['id'],'username':me['username'],'email':me['email'],'counts':summary,'total':sum(summary.values())},indent=2)+'\n')

import { useParams } from 'react-router-dom';
import { FinanceDashboard } from './FinanceDashboard';
import { BodyDashboard } from './BodyDashboard';
import { WellbeingDashboard } from './WellbeingDashboard';

export function SectionDashboardPage() {
  const { section } = useParams();
  if (section === 'finance') return <FinanceDashboard />;
  if (section === 'body') return <BodyDashboard />;
  if (section === 'wellbeing') return <WellbeingDashboard />;
  return <div>Unknown section.</div>;
}

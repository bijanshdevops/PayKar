\d candidate_educations
\d candidate_work_experiences
SELECT column_name FROM information_schema.columns WHERE table_name = 'candidates' AND column_name IN ('email','city');

INSERT INTO storage.buckets (id, name, public) 
VALUES ('logos-tenants', 'logos-tenants', false) 
ON CONFLICT (id) DO NOTHING;

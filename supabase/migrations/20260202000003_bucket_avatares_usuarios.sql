INSERT INTO storage.buckets (id, name, public) 
VALUES ('avatares-usuarios', 'avatares-usuarios', false) 
ON CONFLICT (id) DO NOTHING;

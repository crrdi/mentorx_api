-- Fix: relation "public.actors" does not exist when creating a mentor
-- EF Core creates the table as "Actors" (PascalCase). This replaces the trigger
-- function so it inserts into public."Actors" with the correct column names.
-- Run this in Supabase SQL Editor.

CREATE OR REPLACE FUNCTION public.handle_new_mentor()
RETURNS TRIGGER AS $$
BEGIN
  INSERT INTO public."Actors" ("Id", "Type", "UserId", "MentorId", "CreatedAt", "UpdatedAt")
  VALUES (
    gen_random_uuid(),
    2,  -- ActorType.Mentor = 2
    NULL,
    NEW."Id",
    NOW(),
    NOW()
  );
  RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Trigger is already attached; replacing the function is enough.
-- If you need to recreate the trigger:
-- DROP TRIGGER IF EXISTS on_mentor_created ON public."Mentors";
-- CREATE TRIGGER on_mentor_created
--   AFTER INSERT ON public."Mentors"
--   FOR EACH ROW EXECUTE FUNCTION public.handle_new_mentor();

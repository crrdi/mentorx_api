-- Database Triggers
-- Bu script'i Supabase SQL Editor'de çalıştırın

-- Yeni kullanıcı oluşturulduğunda otomatik olarak users ve actors tablolarına kayıt ekle
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
BEGIN
  -- Users tablosuna kayıt ekle (Entity Framework table/column naming convention)
  INSERT INTO public."Users" (
    "Id", "Email", "Name", "Avatar", "CreatedAt", "UpdatedAt", 
    "DeletedAt", "FocusAreas", "Credits"
  )
  VALUES (
    NEW.id,
    NEW.email,
    COALESCE(NEW.raw_user_meta_data->>'name', 'User'),
    NULL,
    NOW(),
    NOW(),
    NULL,
    '[]',
    10
  );

  -- Actor kaydı ekle (Entity Framework table/column naming convention)
  INSERT INTO public."Actors" ("Id", "Type", "UserId", "MentorId", "CreatedAt", "UpdatedAt")
  VALUES (
    gen_random_uuid(),
    1,  -- ActorType.User = 1
    NEW.id,
    NULL,
    NOW(),
    NOW()
  );

  RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Trigger'ı bağla
DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

-- Counter cache güncellemeleri için trigger'lar
-- Entity Framework table/column naming convention kullanılıyor

-- UserFollowsMentor için follower count güncelleme
CREATE OR REPLACE FUNCTION public.update_mentor_follower_count()
RETURNS TRIGGER AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN
    UPDATE public."Mentors"
    SET "FollowerCount" = "FollowerCount" + 1
    WHERE "Id" = NEW."MentorId";
    RETURN NEW;
  ELSIF TG_OP = 'DELETE' THEN
    UPDATE public."Mentors"
    SET "FollowerCount" = GREATEST("FollowerCount" - 1, 0)
    WHERE "Id" = OLD."MentorId";
    RETURN OLD;
  END IF;
  RETURN NULL;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS update_mentor_follower_count_trigger ON public."UserFollowsMentor";
CREATE TRIGGER update_mentor_follower_count_trigger
  AFTER INSERT OR DELETE ON public."UserFollowsMentor"
  FOR EACH ROW EXECUTE FUNCTION public.update_mentor_follower_count();

-- UserLikes için like count güncelleme
CREATE OR REPLACE FUNCTION public.update_insight_like_count()
RETURNS TRIGGER AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN
    UPDATE public."Insights"
    SET "LikeCount" = "LikeCount" + 1
    WHERE "Id" = NEW."InsightId";
    RETURN NEW;
  ELSIF TG_OP = 'DELETE' THEN
    UPDATE public."Insights"
    SET "LikeCount" = GREATEST("LikeCount" - 1, 0)
    WHERE "Id" = OLD."InsightId";
    RETURN OLD;
  END IF;
  RETURN NULL;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS update_insight_like_count_trigger ON public."UserLikes";
CREATE TRIGGER update_insight_like_count_trigger
  AFTER INSERT OR DELETE ON public."UserLikes"
  FOR EACH ROW EXECUTE FUNCTION public.update_insight_like_count();

-- Comments için comment count güncelleme
CREATE OR REPLACE FUNCTION public.update_insight_comment_count()
RETURNS TRIGGER AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN
    UPDATE public."Insights"
    SET "CommentCount" = "CommentCount" + 1
    WHERE "Id" = NEW."InsightId";
    RETURN NEW;
  ELSIF TG_OP = 'DELETE' THEN
    UPDATE public."Insights"
    SET "CommentCount" = GREATEST("CommentCount" - 1, 0)
    WHERE "Id" = OLD."InsightId";
    RETURN OLD;
  END IF;
  RETURN NULL;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS update_insight_comment_count_trigger ON public."Comments";
CREATE TRIGGER update_insight_comment_count_trigger
  AFTER INSERT OR DELETE ON public."Comments"
  FOR EACH ROW EXECUTE FUNCTION public.update_insight_comment_count();

-- Insights için mentor insight count güncelleme
CREATE OR REPLACE FUNCTION public.update_mentor_insight_count()
RETURNS TRIGGER AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN
    UPDATE public."Mentors"
    SET "InsightCount" = "InsightCount" + 1
    WHERE "Id" = NEW."MentorId";
    RETURN NEW;
  ELSIF TG_OP = 'DELETE' THEN
    UPDATE public."Mentors"
    SET "InsightCount" = GREATEST("InsightCount" - 1, 0)
    WHERE "Id" = OLD."MentorId";
    RETURN OLD;
  END IF;
  RETURN NULL;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS update_mentor_insight_count_trigger ON public."Insights";
CREATE TRIGGER update_mentor_insight_count_trigger
  AFTER INSERT OR DELETE ON public."Insights"
  FOR EACH ROW EXECUTE FUNCTION public.update_mentor_insight_count();

-- Yeni mentor oluşturulduğunda otomatik olarak actors tablosuna kayıt ekle
CREATE OR REPLACE FUNCTION public.handle_new_mentor()
RETURNS TRIGGER AS $$
BEGIN
  -- Actor kaydı ekle (Entity Framework table/column naming convention)
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

-- Trigger'ı bağla
DROP TRIGGER IF EXISTS on_mentor_created ON public."Mentors";
CREATE TRIGGER on_mentor_created
  AFTER INSERT ON public."Mentors"
  FOR EACH ROW EXECUTE FUNCTION public.handle_new_mentor();

-- Database Triggers
-- Bu script'i Supabase SQL Editor'de çalıştırın

-- Yeni kullanıcı oluşturulduğunda otomatik olarak users ve actors tablolarına kayıt ekle
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
BEGIN
  -- Users tablosuna kayıt ekle
  INSERT INTO public.users (
    id, email, name, avatar, "createdAt", "updatedAt", 
    "deletedAt", "focusAreas", credits
  )
  VALUES (
    NEW.id,
    NEW.email,
    COALESCE(NEW.raw_user_meta_data->>'name', 'User'),
    NULL,
    NOW(),
    NOW(),
    NULL,
    '[]'::text[],
    10
  );

  -- Actor kaydı ekle
  INSERT INTO public.actors (id, type, "userId", "mentorId", "createdAt", "updatedAt")
  VALUES (
    gen_random_uuid(),
    'user',
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

-- UserFollowsMentor için follower count güncelleme
CREATE OR REPLACE FUNCTION public.update_mentor_follower_count()
RETURNS TRIGGER AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN
    UPDATE public.mentors
    SET "followerCount" = "followerCount" + 1
    WHERE id = NEW."mentorId";
    RETURN NEW;
  ELSIF TG_OP = 'DELETE' THEN
    UPDATE public.mentors
    SET "followerCount" = GREATEST("followerCount" - 1, 0)
    WHERE id = OLD."mentorId";
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
    UPDATE public.insights
    SET "likeCount" = "likeCount" + 1
    WHERE id = NEW."insightId";
    RETURN NEW;
  ELSIF TG_OP = 'DELETE' THEN
    UPDATE public.insights
    SET "likeCount" = GREATEST("likeCount" - 1, 0)
    WHERE id = OLD."insightId";
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
    UPDATE public.insights
    SET "commentCount" = "commentCount" + 1
    WHERE id = NEW."insightId";
    RETURN NEW;
  ELSIF TG_OP = 'DELETE' THEN
    UPDATE public.insights
    SET "commentCount" = GREATEST("commentCount" - 1, 0)
    WHERE id = OLD."insightId";
    RETURN OLD;
  END IF;
  RETURN NULL;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS update_insight_comment_count_trigger ON public.comments;
CREATE TRIGGER update_insight_comment_count_trigger
  AFTER INSERT OR DELETE ON public.comments
  FOR EACH ROW EXECUTE FUNCTION public.update_insight_comment_count();

-- Insights için mentor insight count güncelleme
CREATE OR REPLACE FUNCTION public.update_mentor_insight_count()
RETURNS TRIGGER AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN
    UPDATE public.mentors
    SET "insightCount" = "insightCount" + 1
    WHERE id = NEW."mentorId";
    RETURN NEW;
  ELSIF TG_OP = 'DELETE' THEN
    UPDATE public.mentors
    SET "insightCount" = GREATEST("insightCount" - 1, 0)
    WHERE id = OLD."mentorId";
    RETURN OLD;
  END IF;
  RETURN NULL;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS update_mentor_insight_count_trigger ON public.insights;
CREATE TRIGGER update_mentor_insight_count_trigger
  AFTER INSERT OR DELETE ON public.insights
  FOR EACH ROW EXECUTE FUNCTION public.update_mentor_insight_count();

-- Yeni mentor oluşturulduğunda otomatik olarak actors tablosuna kayıt ekle
CREATE OR REPLACE FUNCTION public.handle_new_mentor()
RETURNS TRIGGER AS $$
BEGIN
  -- Actor kaydı ekle
  INSERT INTO public.actors (id, type, "userId", "mentorId", "createdAt", "updatedAt")
  VALUES (
    gen_random_uuid(),
    2,  -- ActorType.Mentor = 2
    NULL,
    NEW.id,
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

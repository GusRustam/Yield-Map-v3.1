SELECT 
       Rating.id as id_Rating, 
       Rating.id_RatingAgency,
       RatingAgencyCode.id as id_RatingAgencyCode
       Rating.Value as RatingOrder, 
       Rating.Name as RatingName, 
       RatingAgency.Name as AgencyName
       RatingAgencyCode.Name as AgencyCode
FROM Rating INNER JOIN 
     RatingAgency ON (Rating.id_RatingAgency = RatingAgency.id) INNER JOIN
     RatingAgencyCode ON (RatingAgencyCode.id_RatingAgency = RatingAgency.id)
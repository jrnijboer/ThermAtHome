drop table if exists `FixedSchedule`;

CREATE TABLE `FixedSchedule` (
	`weekday` INT(11) NOT NULL,
	`starttime` TIME NOT NULL,
	`stoptime` TIME NOT NULL,
	`description` VARCHAR(200) NULL DEFAULT NULL,
	`temperature` DECIMAL(3,1) NOT NULL
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB
;

truncate table FixedSchedule;

insert into FixedSchedule (weekday, starttime, stoptime, description, temperature)
values 
/*zondag*/
	(0, "07:00", "07:30", "zondag voor opstaan", 17),
	(0, "07:30", "08:30", "zondag opstaan", 18),
	(0, "08:30", "22:30", "zondag overdag", 19),
	(0, "22:30", "23:50", "zondag voor slapen", 18),
	
/*maandag*/
	(1, "07:00", "07:30", "maandag opstaan", 18),
	(1, "07:30", "09:00", "maandag opwarmen", 18.5),
	(1, "09:00", "22:30", "maandag overdag", 19),
	(1, "22:30", "08:30", "maandag voor slapen", 18),
	
/*dinsdag*/	
	(2, "07:00", "07:30", "dinsdag opstaan", 18),
	(2, "11:30", "22:30", "dinsdag overdag", 19),
	(2, "22:30", "08:30", "dinsdag voor slapen", 18),

/*woensdag*/
	(3, "07:00", "07:30", "woensdag opstaan", 18),
	(3, "07:30", "09:00", "woensdag opwarmen", 18.5),
	(3, "09:00", "22:30", "woensdag overdag", 19),
	(3, "22:30", "08:30", "woensdag voor slapen", 18),
	
/*donderdag*/
	(4, "07:00", "07:30", "donderdag opstaan", 18),
	(4, "07:30", "09:00", "donderdag opwarmen", 18.5),
	(4, "09:00", "22:30", "donderdag overdag", 19),
	(4, "22:30", "08:30", "donderdag voor slapen", 18),
	
/*vrijdag*/	
	(5, "07:00", "07:30", "vrijdag opstaan", 18),
	(5, "15:00", "22:30", "vrijdag overdag", 19),
	(6, "22:30", "08:30", "vrijdag voor slapen", 18),

/*zondag*/
	(6, "07:00", "07:30", "zaterdag voor opstaan", 17),
	(6, "07:30", "08:30", "zaterdag opstaan", 18),
	(6, "08:30", "22:30", "zaterdag overdag", 19),
	(6, "22:30", "23:50", "zaterdag voor slapen", 18);	
	
	select * From FixedSchedule;
	
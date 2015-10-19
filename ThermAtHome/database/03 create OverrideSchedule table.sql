DROP TABLE if exists `OverrideSchedule`;

CREATE TABLE `OverrideSchedule` (
	`overrideday` DATE NOT NULL,
	`starttime` TIME NOT NULL,
	`stoptime` TIME NOT NULL,
	`description` VARCHAR(200) NULL DEFAULT NULL,
	`temperature` DECIMAL(3,1) NOT NULL
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB
;

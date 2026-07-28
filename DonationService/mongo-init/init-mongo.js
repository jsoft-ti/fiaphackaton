// Executed automatically by the official mongo image on first container
// startup (files under /docker-entrypoint-initdb.d/ run once, only against
// an empty data volume). Creates the "donations" collection and every index
// required by the read side, mirroring what MongoIndexInitializer
// (DonationService.Infrastructure) also ensures at application startup -
// having both is deliberate belt-and-suspenders: this script gives a fresh
// `docker compose up` a ready collection immediately, while the hosted
// service keeps it correct even if the database was provisioned another way
// (e.g. a managed MongoDB Atlas cluster where this init script never runs).

const dbName = process.env.MONGO_INITDB_DATABASE || "donation_service";
const database = db.getSiblingDB(dbName);

database.createCollection("donations");

database.donations.createIndex({ campaignId: 1 }, { name: "ix_donations_campaignId" });
database.donations.createIndex({ userId: 1 }, { name: "ix_donations_userId" });
database.donations.createIndex({ donationDate: -1 }, { name: "ix_donations_donationDate" });
database.donations.createIndex({ status: 1 }, { name: "ix_donations_status" });
database.donations.createIndex({ eventId: 1 }, { name: "ux_donations_eventId", unique: true });

print(`DonationService: "donations" collection and indexes ensured on database "${dbName}".`);

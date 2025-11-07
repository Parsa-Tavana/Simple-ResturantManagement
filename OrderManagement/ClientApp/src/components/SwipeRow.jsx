// ClientApp/src/components/SwipeRow.jsx
import { motion, AnimatePresence } from "framer-motion";
import React, { useState } from "react";

export default function SwipeRow({ onDelete, className = "", children, threshold = 120 }) {
    const [visible, setVisible] = useState(true);

    return (
        <AnimatePresence>
            {visible && (
                <motion.tr
                    className={`${className} select-none`}
                    drag="x"
                    dragConstraints={{ left: 0, right: 0 }}   // right-only
                    dragElastic={0.2}
                    dragMomentum
                    whileDrag={{ scale: 0.98 }}
                    initial={{ opacity: 0, y: 6 }}
                    animate={{ opacity: 1, y: 0, transition: { type: "spring", stiffness: 420, damping: 30 } }}
                    exit={{ opacity: 0, x: 300, transition: { duration: 0.2 } }}
                    onDragEnd={async (_e, info) => {
                        if (info.offset.x >= threshold) {
                            setVisible(false);
                            setTimeout(async () => {
                                try { await onDelete?.(); } finally { }
                            }, 180);
                        }
                    }}
                >
                    {children}
                </motion.tr>
            )}
        </AnimatePresence>
    );
}
